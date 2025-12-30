using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Archive.Services;

/// <summary>
/// High-level service for BSA/BA2 archive operations.
/// Note: Full BSA/BA2 support requires external tools or native implementations.
/// This module provides basic operations with plans for full archive support.
/// </summary>
public class ArchiveService
{
    private readonly IModLogger _logger;

    public ArchiveService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get basic information about an archive by reading its header.
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

                // Read offset and counts
                var folderOffset = reader.ReadUInt32();
                var archiveFlags = reader.ReadUInt32();
                var folderCount = reader.ReadUInt32();
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
                return Result<ArchiveInfo>.Fail(
                    "Not a valid BSA/BA2 archive",
                    $"Magic: 0x{magic:X8}");
            }

            return Result<ArchiveInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return Result<ArchiveInfo>.Fail(
                $"Failed to read archive: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// List files in an archive (not yet implemented).
    /// </summary>
    public Result<List<string>> ListFiles(string archivePath, string? filter = null)
    {
        if (!File.Exists(archivePath))
        {
            return Result<List<string>>.Fail($"File not found: {archivePath}");
        }

        return Result<List<string>>.Fail(
            "Archive file listing not yet implemented",
            suggestions: new List<string>
            {
                "Use BSA Browser to view archive contents",
                "Use Archive.exe from Creation Kit"
            });
    }

    /// <summary>
    /// Extract files from an archive (not yet implemented).
    /// </summary>
    public Result<ExtractResult> Extract(string archivePath, string outputDir, string? filter = null)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ExtractResult>.Fail($"File not found: {archivePath}");
        }

        return Result<ExtractResult>.Fail(
            "Archive extraction not yet implemented",
            suggestions: new List<string>
            {
                "Use BSA Browser to extract files",
                "Use Archive.exe from Creation Kit"
            });
    }

    /// <summary>
    /// Create a new BSA archive from a directory (not yet implemented).
    /// </summary>
    public Result<string> Create(string sourceDir, string outputPath, ArchiveCreateOptions? options = null)
    {
        if (!Directory.Exists(sourceDir))
        {
            return Result<string>.Fail($"Directory not found: {sourceDir}");
        }

        return Result<string>.Fail(
            "Archive creation not yet implemented",
            suggestions: new List<string>
            {
                "Use Archive.exe from Creation Kit",
                "Use BSArch tool from Nexus Mods"
            });
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

public class ExtractResult
{
    public int ExtractedCount { get; set; }
    public string OutputDirectory { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}

public class ArchiveCreateOptions
{
    public bool Compress { get; set; } = true;
    public bool ShareData { get; set; } = false;
}
