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
        string? baseDirectory = null,
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

            // Auto-detect base directory if not provided
            if (string.IsNullOrEmpty(baseDirectory) && filesToAdd.Count > 0)
            {
                // Find common parent directory
                baseDirectory = FindCommonParentDirectory(filesToAdd);
                _logger.Debug($"Auto-detected base directory: {baseDirectory}");
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
                    // Calculate relative path from base directory
                    string relativePath;
                    if (!string.IsNullOrEmpty(baseDirectory))
                    {
                        relativePath = Path.GetRelativePath(baseDirectory, sourceFile);

                        // If relative path goes up (..), just use filename
                        if (relativePath.StartsWith(".."))
                        {
                            relativePath = Path.GetFileName(sourceFile);
                        }
                    }
                    else
                    {
                        // No base directory - use just filename
                        relativePath = Path.GetFileName(sourceFile);
                    }

                    var destPath = Path.Combine(tempDir, relativePath);

                    // Create directory if needed
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    File.Copy(sourceFile, destPath, overwrite: true);
                    filesAdded++;
                    _logger.Debug($"Added file: {relativePath}");
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
    /// Compare two archive versions and show differences.
    /// </summary>
    public Result<ArchiveDiffResult> DiffArchives(string archive1Path, string archive2Path)
    {
        if (!File.Exists(archive1Path))
        {
            return Result<ArchiveDiffResult>.Fail($"Archive 1 not found: {archive1Path}");
        }

        if (!File.Exists(archive2Path))
        {
            return Result<ArchiveDiffResult>.Fail($"Archive 2 not found: {archive2Path}");
        }

        try
        {
            // List files from both archives
            var list1Result = ListFiles(archive1Path, limit: 0);
            if (!list1Result.Success)
            {
                return Result<ArchiveDiffResult>.Fail($"Failed to list archive 1: {list1Result.Error}");
            }

            var list2Result = ListFiles(archive2Path, limit: 0);
            if (!list2Result.Success)
            {
                return Result<ArchiveDiffResult>.Fail($"Failed to list archive 2: {list2Result.Error}");
            }

            var files1 = list1Result.Value!.ToDictionary(f => f.Path, f => f);
            var files2 = list2Result.Value!.ToDictionary(f => f.Path, f => f);

            // Find differences
            var added = new List<string>();
            var removed = new List<string>();
            var modified = new List<string>();
            var unchanged = new List<string>();

            // Files in archive 2 but not in archive 1 = added
            foreach (var path in files2.Keys)
            {
                if (!files1.ContainsKey(path))
                {
                    added.Add(path);
                }
            }

            // Files in archive 1 but not in archive 2 = removed
            foreach (var path in files1.Keys)
            {
                if (!files2.ContainsKey(path))
                {
                    removed.Add(path);
                }
            }

            // Files in both - check if modified
            foreach (var path in files1.Keys)
            {
                if (files2.ContainsKey(path))
                {
                    var file1 = files1[path];
                    var file2 = files2[path];

                    // Compare sizes to detect modifications
                    if (file1.Size != file2.Size)
                    {
                        modified.Add(path);
                    }
                    else
                    {
                        unchanged.Add(path);
                    }
                }
            }

            return Result<ArchiveDiffResult>.Ok(new ArchiveDiffResult
            {
                Archive1 = Path.GetFileName(archive1Path),
                Archive2 = Path.GetFileName(archive2Path),
                FilesAdded = added,
                FilesRemoved = removed,
                FilesModified = modified,
                FilesUnchanged = unchanged,
                TotalFiles1 = files1.Count,
                TotalFiles2 = files2.Count
            });
        }
        catch (Exception ex)
        {
            return Result<ArchiveDiffResult>.Fail(
                $"Failed to compare archives: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Optimize an archive by repacking with compression.
    /// </summary>
    public async Task<Result<ArchiveOptimizeResult>> OptimizeAsync(
        string archivePath,
        string? outputPath = null,
        ArchiveCreateOptions? options = null)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveOptimizeResult>.Fail($"Archive not found: {archivePath}");
        }

        // Default output path: overwrite original
        outputPath ??= archivePath;

        string? tempDir = null;
        string? tempOutput = null;

        try
        {
            // Get original info
            var infoResult = GetInfo(archivePath);
            if (!infoResult.Success)
            {
                return Result<ArchiveOptimizeResult>.Fail($"Failed to read archive info: {infoResult.Error}");
            }

            var originalInfo = infoResult.Value!;
            var originalSize = new FileInfo(archivePath).Length;

            // Extract to temp
            tempDir = Path.Combine(Path.GetTempPath(), $"archive-optimize-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            _logger.Debug($"Extracting archive to: {tempDir}");

            var extractResult = await ExtractAsync(archivePath, tempDir);
            if (!extractResult.Success)
            {
                return Result<ArchiveOptimizeResult>.Fail($"Failed to extract archive: {extractResult.Error}");
            }

            // If overwriting, create temp output first
            if (outputPath.Equals(archivePath, StringComparison.OrdinalIgnoreCase))
            {
                tempOutput = Path.Combine(Path.GetTempPath(), $"archive-optimize-output-{Guid.NewGuid()}.bsa");
            }
            else
            {
                tempOutput = outputPath;
            }

            // Repack with compression
            options ??= new ArchiveCreateOptions { Compress = true };

            _logger.Debug($"Repacking archive with compression: {options.Compress}");

            var createResult = await CreateAsync(tempDir, tempOutput, options);
            if (!createResult.Success)
            {
                return Result<ArchiveOptimizeResult>.Fail($"Failed to repack archive: {createResult.Error}");
            }

            // If we used temp output, replace original
            if (!tempOutput.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(archivePath);
                File.Move(tempOutput, outputPath);
            }

            var newSize = new FileInfo(outputPath).Length;
            var savings = originalSize - newSize;
            var savingsPercent = originalSize > 0 ? (double)savings / originalSize * 100 : 0;

            return Result<ArchiveOptimizeResult>.Ok(new ArchiveOptimizeResult
            {
                OutputPath = outputPath,
                OriginalSize = originalSize,
                OptimizedSize = newSize,
                Savings = savings,
                SavingsPercent = savingsPercent,
                FileCount = originalInfo.FileCount
            });
        }
        catch (Exception ex)
        {
            return Result<ArchiveOptimizeResult>.Fail(
                $"Failed to optimize archive: {ex.Message}",
                ex.StackTrace);
        }
        finally
        {
            // Cleanup
            if (tempDir != null && Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup temp directory: {ex.Message}");
                }
            }

            if (tempOutput != null && File.Exists(tempOutput) && !tempOutput.Equals(outputPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.Delete(tempOutput);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup temp output: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Validate archive integrity.
    /// Checks if archive is readable and files can be listed.
    /// </summary>
    public Result<ArchiveValidationResult> Validate(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveValidationResult>.Fail($"Archive not found: {archivePath}");
        }

        var issues = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Check if we can get basic info
            var infoResult = GetInfo(archivePath);
            if (!infoResult.Success)
            {
                issues.Add($"Failed to read archive header: {infoResult.Error}");
                return Result<ArchiveValidationResult>.Ok(new ArchiveValidationResult
                {
                    IsValid = false,
                    Issues = issues,
                    Warnings = warnings
                });
            }

            var info = infoResult.Value!;

            // Check if we can list files
            var listResult = ListFiles(archivePath, limit: 0);
            if (!listResult.Success)
            {
                issues.Add($"Failed to list archive files: {listResult.Error}");
                return Result<ArchiveValidationResult>.Ok(new ArchiveValidationResult
                {
                    IsValid = false,
                    FileCount = info.FileCount,
                    ArchiveSize = info.FileSize,
                    Issues = issues,
                    Warnings = warnings
                });
            }

            var files = listResult.Value!;

            // Check if file count matches
            if (files.Count != info.FileCount)
            {
                warnings.Add($"File count mismatch: header says {info.FileCount}, but found {files.Count}");
            }

            // Check for suspicious file sizes
            foreach (var file in files)
            {
                if (file.Size == 0)
                {
                    warnings.Add($"Zero-byte file: {file.Path}");
                }
            }

            // Archive is valid if we could read it
            var isValid = issues.Count == 0;

            return Result<ArchiveValidationResult>.Ok(new ArchiveValidationResult
            {
                IsValid = isValid,
                FileCount = files.Count,
                ArchiveSize = info.FileSize,
                ArchiveType = info.Type,
                Issues = issues,
                Warnings = warnings
            });
        }
        catch (Exception ex)
        {
            issues.Add($"Validation failed: {ex.Message}");
            return Result<ArchiveValidationResult>.Ok(new ArchiveValidationResult
            {
                IsValid = false,
                Issues = issues,
                Warnings = warnings
            });
        }
    }

    /// <summary>
    /// Merge multiple archives into one.
    /// Later archives overwrite earlier ones on conflict.
    /// </summary>
    public async Task<Result<ArchiveMergeResult>> MergeArchivesAsync(
        List<string> archivePaths,
        string outputPath,
        ArchiveCreateOptions? options = null)
    {
        if (archivePaths == null || archivePaths.Count == 0)
        {
            return Result<ArchiveMergeResult>.Fail("No archives specified to merge");
        }

        // Verify all archives exist
        foreach (var archive in archivePaths)
        {
            if (!File.Exists(archive))
            {
                return Result<ArchiveMergeResult>.Fail($"Archive not found: {archive}");
            }
        }

        string? mergeDir = null;
        var conflicts = new List<string>();
        var filesPerArchive = new Dictionary<string, int>();

        try
        {
            // Create merge directory
            mergeDir = Path.Combine(Path.GetTempPath(), $"archive-merge-{Guid.NewGuid()}");
            Directory.CreateDirectory(mergeDir);

            _logger.Debug($"Merging {archivePaths.Count} archives to: {mergeDir}");

            // Extract each archive and copy files (later archives overwrite earlier)
            foreach (var archive in archivePaths)
            {
                var archiveName = Path.GetFileName(archive);
                var tempExtractDir = Path.Combine(Path.GetTempPath(), $"archive-merge-extract-{Guid.NewGuid()}");

                try
                {
                    _logger.Debug($"Extracting: {archiveName}");

                    var extractResult = await ExtractAsync(archive, tempExtractDir);
                    if (!extractResult.Success)
                    {
                        return Result<ArchiveMergeResult>.Fail($"Failed to extract {archiveName}: {extractResult.Error}");
                    }

                    // Copy files from extract dir to merge dir
                    var files = Directory.GetFiles(tempExtractDir, "*", SearchOption.AllDirectories);
                    var fileCount = 0;

                    foreach (var sourceFile in files)
                    {
                        var relativePath = Path.GetRelativePath(tempExtractDir, sourceFile);
                        var destFile = Path.Combine(mergeDir, relativePath);

                        // Track conflicts
                        if (File.Exists(destFile))
                        {
                            conflicts.Add($"{relativePath} (overwritten by {archiveName})");
                        }

                        var destDir = Path.GetDirectoryName(destFile);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        File.Copy(sourceFile, destFile, overwrite: true);
                        fileCount++;
                    }

                    filesPerArchive[archiveName] = fileCount;
                    _logger.Debug($"Merged {fileCount} files from {archiveName}");
                }
                finally
                {
                    // Cleanup extract directory
                    if (Directory.Exists(tempExtractDir))
                    {
                        try
                        {
                            Directory.Delete(tempExtractDir, recursive: true);
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug($"Failed to cleanup temp extract directory: {ex.Message}");
                        }
                    }
                }
            }

            // Create merged archive
            _logger.Debug($"Creating merged archive: {outputPath}");

            var createResult = await CreateAsync(mergeDir, outputPath, options);
            if (!createResult.Success)
            {
                return Result<ArchiveMergeResult>.Fail($"Failed to create merged archive: {createResult.Error}");
            }

            var totalFiles = Directory.GetFiles(mergeDir, "*", SearchOption.AllDirectories).Length;

            return Result<ArchiveMergeResult>.Ok(new ArchiveMergeResult
            {
                OutputPath = outputPath,
                ArchivesMerged = archivePaths.Count,
                TotalFiles = totalFiles,
                Conflicts = conflicts,
                FilesPerArchive = filesPerArchive
            });
        }
        catch (Exception ex)
        {
            return Result<ArchiveMergeResult>.Fail(
                $"Failed to merge archives: {ex.Message}",
                ex.StackTrace);
        }
        finally
        {
            // Cleanup merge directory
            if (mergeDir != null && Directory.Exists(mergeDir))
            {
                try
                {
                    Directory.Delete(mergeDir, recursive: true);
                    _logger.Debug($"Cleaned up merge directory: {mergeDir}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup merge directory: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Extract a single file from an archive.
    /// </summary>
    public async Task<Result<string>> ExtractFileAsync(
        string archivePath,
        string targetFile,
        string outputPath)
    {
        if (!File.Exists(archivePath))
        {
            return Result<string>.Fail($"Archive not found: {archivePath}");
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            return Result<string>.Fail("Target file path not specified");
        }

        string? tempDir = null;
        try
        {
            // Extract to temp directory
            tempDir = Path.Combine(Path.GetTempPath(), $"archive-extract-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            _logger.Debug($"Extracting archive to temp: {tempDir}");

            var extractResult = await ExtractAsync(archivePath, tempDir);
            if (!extractResult.Success)
            {
                return Result<string>.Fail($"Failed to extract archive: {extractResult.Error}");
            }

            // Find the target file
            var sourceFile = Path.Combine(tempDir, targetFile);
            if (!File.Exists(sourceFile))
            {
                // Try normalizing path separators
                sourceFile = Path.Combine(tempDir, targetFile.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
            }

            if (!File.Exists(sourceFile))
            {
                return Result<string>.Fail(
                    $"File not found in archive: {targetFile}",
                    suggestions: new List<string>
                    {
                        "Use 'archive list' to see available files",
                        "Check the file path spelling and case"
                    });
            }

            // Copy to output location
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.Copy(sourceFile, outputPath, overwrite: true);
            _logger.Debug($"Extracted file to: {outputPath}");

            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to extract file: {ex.Message}",
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
    /// Update a single file in an existing archive.
    /// Convenience wrapper around ReplaceFilesAsync for single file updates.
    /// </summary>
    public async Task<Result<ArchiveEditResult>> UpdateFileAsync(
        string archivePath,
        string targetFile,
        string sourceFile,
        bool preserveCompression = true)
    {
        if (!File.Exists(archivePath))
        {
            return Result<ArchiveEditResult>.Fail($"Archive not found: {archivePath}");
        }

        if (!File.Exists(sourceFile))
        {
            return Result<ArchiveEditResult>.Fail($"Source file not found: {sourceFile}");
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            return Result<ArchiveEditResult>.Fail("Target file path not specified");
        }

        // Create temp directory with the single file in correct structure
        var tempDir = Path.Combine(Path.GetTempPath(), $"archive-update-{Guid.NewGuid()}");
        try
        {
            Directory.CreateDirectory(tempDir);

            // Copy source file to temp with target file's path
            var destPath = Path.Combine(tempDir, targetFile);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(sourceFile, destPath, overwrite: true);

            // Use ReplaceFilesAsync with specific filter
            var result = await ReplaceFilesAsync(archivePath, tempDir, targetFile, preserveCompression);

            return result;
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to cleanup temp directory: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Find the common parent directory for a list of files.
    /// </summary>
    private string FindCommonParentDirectory(List<string> files)
    {
        if (files.Count == 0)
            return string.Empty;

        if (files.Count == 1)
            return Path.GetDirectoryName(files[0]) ?? string.Empty;

        // Get all directory paths
        var directories = files.Select(f => Path.GetDirectoryName(Path.GetFullPath(f)) ?? string.Empty).ToList();

        // Split paths into segments
        var pathSegments = directories.Select(d => d.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).ToList();

        // Find common prefix
        var commonSegments = new List<string>();
        var minLength = pathSegments.Min(p => p.Length);

        for (int i = 0; i < minLength; i++)
        {
            var segment = pathSegments[0][i];
            if (pathSegments.All(p => p[i].Equals(segment, StringComparison.OrdinalIgnoreCase)))
            {
                commonSegments.Add(segment);
            }
            else
            {
                break;
            }
        }

        return string.Join(Path.DirectorySeparatorChar.ToString(), commonSegments);
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

public class ArchiveMergeResult
{
    public string OutputPath { get; set; } = "";
    public int ArchivesMerged { get; set; }
    public int TotalFiles { get; set; }
    public List<string> Conflicts { get; set; } = new();
    public Dictionary<string, int> FilesPerArchive { get; set; } = new();
}

public class ArchiveValidationResult
{
    public bool IsValid { get; set; }
    public int FileCount { get; set; }
    public long ArchiveSize { get; set; }
    public string ArchiveType { get; set; } = "";
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ArchiveOptimizeResult
{
    public string OutputPath { get; set; } = "";
    public long OriginalSize { get; set; }
    public long OptimizedSize { get; set; }
    public long Savings { get; set; }
    public double SavingsPercent { get; set; }
    public int FileCount { get; set; }
}

public class ArchiveDiffResult
{
    public string Archive1 { get; set; } = "";
    public string Archive2 { get; set; } = "";
    public List<string> FilesAdded { get; set; } = new();
    public List<string> FilesRemoved { get; set; } = new();
    public List<string> FilesModified { get; set; } = new();
    public List<string> FilesUnchanged { get; set; } = new();
    public int TotalFiles1 { get; set; }
    public int TotalFiles2 { get; set; }
}
