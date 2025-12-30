using SpookysAutomod.Archive.CliWrappers;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Archive.Services;

/// <summary>
/// High-level service for BSA/BA2 archive operations.
/// Uses BSArch CLI for creation/extraction.
/// </summary>
public class ArchiveService
{
    private readonly IModLogger _logger;
    private readonly BsarchWrapper _bsarch;

    public ArchiveService(IModLogger logger)
    {
        _logger = logger;
        _bsarch = new BsarchWrapper(logger);
    }

    /// <summary>
    /// Get information about an archive by reading its header.
    /// </summary>
    public Result<ArchiveInfo> GetInfo(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveInfo>.Fail($"File not found: {archivePath}");
        }

        try
        {
            using var stream = File.OpenRead(archivePath);
            using var reader = new BinaryReader(stream);

            var magic = reader.ReadUInt32();
            var info = new ArchiveInfo
            {
                FilePath = archivePath,
                FileName = Path.GetFileName(archivePath),
                FileSize = new FileInfo(archivePath).Length
            };

            // BSA magic: 0x00415342 ('BSA\0')
            // BA2 magic: 0x58445442 ('BTDX')
            if (magic == 0x00415342)
            {
                info.Type = "BSA";
                var version = reader.ReadUInt32();
                info.Version = version.ToString();
                reader.ReadUInt32(); // folder offset
                reader.ReadUInt32(); // archive flags
                reader.ReadUInt32(); // folder count
                var fileCount = reader.ReadUInt32();
                info.FileCount = (int)fileCount;
            }
            else if (magic == 0x58445442)
            {
                info.Type = "BA2";
                var version = reader.ReadUInt32();
                info.Version = version.ToString();
                var type = reader.ReadBytes(4);
                var typeStr = System.Text.Encoding.ASCII.GetString(type);
                info.Type = $"BA2 ({typeStr.Trim('\0')})";
                var fileCount = reader.ReadUInt32();
                info.FileCount = (int)fileCount;
            }
            else
            {
                return Result<ArchiveInfo>.Fail($"Not a valid BSA/BA2 archive (magic: 0x{magic:X8})");
            }

            return Result<ArchiveInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return Result<ArchiveInfo>.Fail($"Failed to read archive: {ex.Message}");
        }
    }

    /// <summary>
    /// List files in an archive by reading BSA/BA2 file tables.
    /// </summary>
    public Result<List<ArchiveFileEntry>> ListFiles(string archivePath, string? filter = null, int? limit = null)
    {
        if (!File.Exists(archivePath))
        {
            return Result<List<ArchiveFileEntry>>.Fail($"File not found: {archivePath}");
        }

        try
        {
            using var stream = File.OpenRead(archivePath);
            using var reader = new BinaryReader(stream);

            var magic = reader.ReadUInt32();

            if (magic == 0x00415342) // BSA
            {
                return ReadBsaFileList(reader, filter, limit);
            }
            else if (magic == 0x58445442) // BA2
            {
                return ReadBa2FileList(reader, filter, limit);
            }
            else
            {
                return Result<List<ArchiveFileEntry>>.Fail($"Not a valid BSA/BA2 archive");
            }
        }
        catch (Exception ex)
        {
            return Result<List<ArchiveFileEntry>>.Fail($"Failed to list archive: {ex.Message}");
        }
    }

    private Result<List<ArchiveFileEntry>> ReadBsaFileList(BinaryReader reader, string? filter, int? limit)
    {
        var entries = new List<ArchiveFileEntry>();

        // Read BSA header (we already read magic)
        var version = reader.ReadUInt32();
        var folderOffset = reader.ReadUInt32();
        var archiveFlags = reader.ReadUInt32();
        var folderCount = reader.ReadUInt32();
        var fileCount = reader.ReadUInt32();
        var totalFolderNameLength = reader.ReadUInt32();
        var totalFileNameLength = reader.ReadUInt32();
        var fileFlags = reader.ReadUInt32();

        bool hasFileNames = (archiveFlags & 0x2) != 0;
        bool compressedByDefault = (archiveFlags & 0x4) != 0;
        bool isSse = version == 105; // SSE uses version 105

        if (!hasFileNames)
        {
            return Result<List<ArchiveFileEntry>>.Fail(
                "Archive does not contain file names",
                suggestions: new List<string> { "Use BSA Browser or extract the archive" });
        }

        // Read folder records
        var folderRecords = new List<(ulong hash, uint count, ulong offset)>();
        for (int i = 0; i < folderCount; i++)
        {
            var hash = reader.ReadUInt64();
            var count = reader.ReadUInt32();
            if (isSse)
            {
                reader.ReadUInt32(); // unknown padding
                var offset = reader.ReadUInt64();
                folderRecords.Add((hash, count, offset));
            }
            else
            {
                var offset = reader.ReadUInt32();
                folderRecords.Add((hash, count, offset));
            }
        }

        // Read file record blocks (each folder has a name followed by file records)
        var folderNames = new List<string>();
        var fileRecords = new List<(string folder, ulong hash, uint size, uint offset)>();

        foreach (var folder in folderRecords)
        {
            // Read folder name (length-prefixed string)
            var nameLen = reader.ReadByte();
            var nameBytes = reader.ReadBytes(nameLen);
            var folderName = System.Text.Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
            folderNames.Add(folderName);

            // Read file records for this folder
            for (int i = 0; i < folder.count; i++)
            {
                var hash = reader.ReadUInt64();
                var size = reader.ReadUInt32();
                var offset = reader.ReadUInt32();
                fileRecords.Add((folderName, hash, size, offset));
            }
        }

        // Read file names block
        var fileNamesStart = reader.BaseStream.Position;
        var fileNamesData = reader.ReadBytes((int)totalFileNameLength);
        var fileNames = System.Text.Encoding.ASCII.GetString(fileNamesData)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);

        // Build file entries
        for (int i = 0; i < fileRecords.Count && i < fileNames.Length; i++)
        {
            var record = fileRecords[i];
            var fileName = fileNames[i];
            var fullPath = string.IsNullOrEmpty(record.folder)
                ? fileName
                : $"{record.folder}\\{fileName}";

            // Check filter
            if (!string.IsNullOrEmpty(filter))
            {
                if (!fullPath.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                    !MatchesGlobPattern(fullPath, filter))
                {
                    continue;
                }
            }

            // Check compression flag (bit 30 of size toggles default compression)
            var rawSize = record.size;
            var isCompressed = compressedByDefault;
            if ((rawSize & 0x40000000) != 0)
            {
                isCompressed = !isCompressed;
                rawSize &= 0x3FFFFFFF;
            }

            entries.Add(new ArchiveFileEntry
            {
                Path = fullPath,
                Size = rawSize,
                IsCompressed = isCompressed
            });

            if (limit.HasValue && entries.Count >= limit.Value)
                break;
        }

        return Result<List<ArchiveFileEntry>>.Ok(entries);
    }

    private Result<List<ArchiveFileEntry>> ReadBa2FileList(BinaryReader reader, string? filter, int? limit)
    {
        var entries = new List<ArchiveFileEntry>();

        // BA2 header
        var version = reader.ReadUInt32();
        var typeBytes = reader.ReadBytes(4);
        var type = System.Text.Encoding.ASCII.GetString(typeBytes).Trim('\0');
        var fileCount = reader.ReadUInt32();
        var nameTableOffset = reader.ReadUInt64();

        // For GNRL (general) BA2, file records are 36 bytes each
        // For DX10 (texture) BA2, they're different
        if (type != "GNRL")
        {
            // For texture BA2s, we can still read the name table
            reader.BaseStream.Seek((long)nameTableOffset, SeekOrigin.Begin);
        }
        else
        {
            // Skip file records and go to name table
            reader.BaseStream.Seek((long)nameTableOffset, SeekOrigin.Begin);
        }

        // Read name table
        for (int i = 0; i < fileCount; i++)
        {
            var nameLen = reader.ReadUInt16();
            var nameBytes = reader.ReadBytes(nameLen);
            var fileName = System.Text.Encoding.ASCII.GetString(nameBytes);

            // Check filter
            if (!string.IsNullOrEmpty(filter))
            {
                if (!fileName.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                    !MatchesGlobPattern(fileName, filter))
                {
                    continue;
                }
            }

            entries.Add(new ArchiveFileEntry
            {
                Path = fileName,
                Size = 0, // Would need to read file records for size
                IsCompressed = true // BA2 files are typically compressed
            });

            if (limit.HasValue && entries.Count >= limit.Value)
                break;
        }

        return Result<List<ArchiveFileEntry>>.Ok(entries);
    }

    private static bool MatchesGlobPattern(string path, string pattern)
    {
        // Simple glob matching for *.ext patterns
        if (pattern.StartsWith("*"))
        {
            var ext = pattern.Substring(1);
            return path.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
        }
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern.Substring(0, pattern.Length - 1);
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        if (pattern.Contains("*"))
        {
            var parts = pattern.Split('*');
            var idx = 0;
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                var found = path.IndexOf(part, idx, StringComparison.OrdinalIgnoreCase);
                if (found < idx) return false;
                idx = found + part.Length;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Extract files from an archive using BSArch.
    /// </summary>
    public async Task<Result<ExtractResult>> ExtractAsync(string archivePath, string outputDir, string? filter = null)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ExtractResult>.Fail($"File not found: {archivePath}");
        }

        _logger.Debug($"Extracting archive: {archivePath} to {outputDir}");
        var bsarchResult = await _bsarch.UnpackAsync(archivePath, outputDir);

        if (bsarchResult.Success)
        {
            // Count extracted files
            var extractedCount = 0;
            if (Directory.Exists(outputDir))
            {
                extractedCount = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories).Length;
            }

            return Result<ExtractResult>.Ok(new ExtractResult
            {
                OutputDirectory = outputDir,
                ExtractedCount = extractedCount
            });
        }

        return Result<ExtractResult>.Fail(bsarchResult.Error!);
    }

    /// <summary>
    /// Create a BSA archive from a directory using BSArch.
    /// </summary>
    public async Task<Result<string>> CreateAsync(string sourceDir, string outputPath, ArchiveCreateOptions? options = null)
    {
        if (!Directory.Exists(sourceDir))
        {
            return Result<string>.Fail($"Directory not found: {sourceDir}");
        }

        options ??= new ArchiveCreateOptions();

        var bsarchOptions = new BsarchOptions
        {
            GameType = options.GameType,
            Compress = options.Compress,
            Multithreaded = true
        };

        var result = await _bsarch.PackAsync(sourceDir, outputPath, bsarchOptions);

        if (result.Success)
        {
            _logger.Info($"Created archive: {outputPath}");
        }

        return result;
    }

    /// <summary>
    /// Check if BSArch is available.
    /// </summary>
    public async Task<Result<string>> CheckToolsAsync()
    {
        return await _bsarch.EnsureAvailableAsync();
    }
}

public class ArchiveInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string Type { get; set; } = "";
    public string Version { get; set; } = "";
    public int FileCount { get; set; }
}

public class ArchiveFileEntry
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public long CompressedSize { get; set; }
    public bool IsCompressed { get; set; }
}

public class ExtractResult
{
    public int ExtractedCount { get; set; }
    public string OutputDirectory { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}

public class ArchiveCreateOptions
{
    public GameType GameType { get; set; } = GameType.SkyrimSE;
    public bool Compress { get; set; } = true;
}
