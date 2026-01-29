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
    /// Add files to an existing archive.
    /// Uses extract-modify-repack workflow.
    /// </summary>
    public async Task<Result<ArchiveEditResult>> AddFilesAsync(
        string archivePath,
        List<string> filesToAdd,
        bool preserveCompression = true)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveEditResult>.Fail($"Archive not found: {archivePath}");
        }

        if (filesToAdd == null || filesToAdd.Count == 0)
        {
            return Result<ArchiveEditResult>.Fail("No files specified to add");
        }

        string? tempDir = null;
        try
        {
            // Get original archive settings
            var infoResult = GetInfo(archivePath);
            if (!infoResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to read archive info: {infoResult.Error}");
            }

            var archiveInfo = infoResult.Value!;
            var originalCompression = preserveCompression; // Default to preserving compression

            // Create temp directory
            tempDir = Path.Combine(Path.GetTempPath(), $"archive-edit-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            _logger.Debug($"Extracting archive to temp: {tempDir}");

            // Extract existing archive
            var extractResult = await ExtractAsync(archivePath, tempDir);
            if (!extractResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to extract archive: {extractResult.Error}");
            }

            // Copy new files to temp directory
            var filesAdded = 0;
            var errors = new List<string>();

            foreach (var sourceFile in filesToAdd)
            {
                if (!File.Exists(sourceFile))
                {
                    errors.Add($"Source file not found: {sourceFile}");
                    continue;
                }

                try
                {
                    // Determine destination path (relative to temp dir)
                    var fileName = Path.GetFileName(sourceFile);
                    var destPath = Path.Combine(tempDir, fileName);

                    // Create directory if needed
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    File.Copy(sourceFile, destPath, overwrite: true);
                    filesAdded++;
                    _logger.Debug($"Added file: {fileName}");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to copy {sourceFile}: {ex.Message}");
                }
            }

            if (filesAdded == 0)
            {
                return Result<ArchiveEditResult>.Fail(
                    "No files were added",
                    suggestions: new List<string> { "Check that source files exist and are accessible" });
            }

            // Repack archive
            var options = new ArchiveCreateOptions
            {
                Compress = originalCompression
            };

            var createResult = await CreateAsync(tempDir, archivePath, options);
            if (!createResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to repack archive: {createResult.Error}");
            }

            return Result<ArchiveEditResult>.Ok(new ArchiveEditResult
            {
                FilesModified = filesAdded,
                TotalFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories).Length,
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            return Result<ArchiveEditResult>.Fail(
                $"Failed to add files: {ex.Message}",
                ex.StackTrace);
        }
        finally
        {
            // Cleanup temp directory
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                    _logger.Debug($"Cleaned up temp directory: {tempDir}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup temp directory: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Remove files from an existing archive.
    /// Uses extract-modify-repack workflow.
    /// </summary>
    public async Task<Result<ArchiveEditResult>> RemoveFilesAsync(
        string archivePath,
        string? filter = null,
        bool preserveCompression = true)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveEditResult>.Fail($"Archive not found: {archivePath}");
        }

        if (string.IsNullOrEmpty(filter))
        {
            return Result<ArchiveEditResult>.Fail(
                "No filter specified",
                suggestions: new List<string>
                {
                    "Use --filter to specify which files to remove",
                    "Example: --filter '*.esp' or --filter 'scripts/*'"
                });
        }

        string? tempDir = null;
        try
        {
            // Create temp directory
            tempDir = Path.Combine(Path.GetTempPath(), $"archive-edit-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            _logger.Debug($"Extracting archive to temp: {tempDir}");

            // Extract existing archive
            var extractResult = await ExtractAsync(archivePath, tempDir);
            if (!extractResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to extract archive: {extractResult.Error}");
            }

            // Remove matching files
            var filesRemoved = 0;
            var allFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                var relativePath = Path.GetRelativePath(tempDir, file);

                if (MatchesFilter(relativePath, filter))
                {
                    File.Delete(file);
                    filesRemoved++;
                    _logger.Debug($"Removed file: {relativePath}");
                }
            }

            if (filesRemoved == 0)
            {
                return Result<ArchiveEditResult>.Fail(
                    "No files matched the filter",
                    suggestions: new List<string>
                    {
                        "Check the filter pattern",
                        "Use 'archive list' to see available files"
                    });
            }

            // Repack archive
            var options = new ArchiveCreateOptions
            {
                Compress = preserveCompression
            };

            var createResult = await CreateAsync(tempDir, archivePath, options);
            if (!createResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to repack archive: {createResult.Error}");
            }

            var remainingFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories).Length;

            return Result<ArchiveEditResult>.Ok(new ArchiveEditResult
            {
                FilesModified = filesRemoved,
                TotalFiles = remainingFiles,
                Errors = new List<string>()
            });
        }
        catch (Exception ex)
        {
            return Result<ArchiveEditResult>.Fail(
                $"Failed to remove files: {ex.Message}",
                ex.StackTrace);
        }
        finally
        {
            // Cleanup temp directory
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                    _logger.Debug($"Cleaned up temp directory: {tempDir}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup temp directory: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Replace files in an existing archive.
    /// Uses extract-modify-repack workflow.
    /// </summary>
    public async Task<Result<ArchiveEditResult>> ReplaceFilesAsync(
        string archivePath,
        string sourceDir,
        string? filter = null,
        bool preserveCompression = true)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveEditResult>.Fail($"Archive not found: {archivePath}");
        }

        if (!Directory.Exists(sourceDir))
        {
            return Result<ArchiveEditResult>.Fail($"Source directory not found: {sourceDir}");
        }

        string? tempDir = null;
        try
        {
            // Create temp directory
            tempDir = Path.Combine(Path.GetTempPath(), $"archive-edit-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            _logger.Debug($"Extracting archive to temp: {tempDir}");

            // Extract existing archive
            var extractResult = await ExtractAsync(archivePath, tempDir);
            if (!extractResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to extract archive: {extractResult.Error}");
            }

            // Replace matching files from source directory
            var filesReplaced = 0;
            var errors = new List<string>();
            var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir, sourceFile);

                // Check filter if specified
                if (!string.IsNullOrEmpty(filter) && !MatchesFilter(relativePath, filter))
                {
                    continue;
                }

                try
                {
                    var destPath = Path.Combine(tempDir, relativePath);

                    // Only replace if file exists in archive
                    if (File.Exists(destPath))
                    {
                        File.Copy(sourceFile, destPath, overwrite: true);
                        filesReplaced++;
                        _logger.Debug($"Replaced file: {relativePath}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to replace {relativePath}: {ex.Message}");
                }
            }

            if (filesReplaced == 0)
            {
                return Result<ArchiveEditResult>.Fail(
                    "No files were replaced",
                    suggestions: new List<string>
                    {
                        "Check that source files match existing archive files",
                        "Use 'archive list' to see files in the archive",
                        "Adjust the filter pattern if needed"
                    });
            }

            // Repack archive
            var options = new ArchiveCreateOptions
            {
                Compress = preserveCompression
            };

            var createResult = await CreateAsync(tempDir, archivePath, options);
            if (!createResult.Success)
            {
                return Result<ArchiveEditResult>.Fail($"Failed to repack archive: {createResult.Error}");
            }

            var totalFiles = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories).Length;

            return Result<ArchiveEditResult>.Ok(new ArchiveEditResult
            {
                FilesModified = filesReplaced,
                TotalFiles = totalFiles,
                Errors = errors
            });
        }
        catch (Exception ex)
        {
            return Result<ArchiveEditResult>.Fail(
                $"Failed to replace files: {ex.Message}",
                ex.StackTrace);
        }
        finally
        {
            // Cleanup temp directory
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                    _logger.Debug($"Cleaned up temp directory: {tempDir}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup temp directory: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Simple pattern matching for file filters.
    /// Supports wildcards like *.ext or folder/* patterns.
    /// </summary>
    private bool MatchesFilter(string path, string filter)
    {
        // Normalize path separators
        path = path.Replace('\\', '/');
        filter = filter.Replace('\\', '/');

        // Exact match
        if (path.Equals(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        // Wildcard patterns
        if (filter.Contains('*'))
        {
            // Convert glob pattern to regex
            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(filter)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                + "$";

            return System.Text.RegularExpressions.Regex.IsMatch(
                path,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Substring match
        return path.Contains(filter, StringComparison.OrdinalIgnoreCase);
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

public class ArchiveEditResult
{
    public int FilesModified { get; set; }
    public int TotalFiles { get; set; }
    public List<string> Errors { get; set; } = new();
}
