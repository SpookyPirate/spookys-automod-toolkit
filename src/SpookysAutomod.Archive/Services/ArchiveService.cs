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
    /// List files in an archive (requires BSArch).
    /// </summary>
    public Result<List<ArchiveFileEntry>> ListFiles(string archivePath, string? filter = null)
    {
        if (!File.Exists(archivePath))
        {
            return Result<List<ArchiveFileEntry>>.Fail($"File not found: {archivePath}");
        }

        return Result<List<ArchiveFileEntry>>.Fail(
            "Archive file listing requires extraction. Use 'archive extract' command.",
            suggestions: new List<string>
            {
                "Use BSA Browser for browsing archive contents",
                "Extract the archive to list files"
            });
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
