using System.CommandLine;
using SpookysAutomod.Archive.CliWrappers;
using SpookysAutomod.Archive.Services;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Cli.Commands;

public static class ArchiveCommands
{
    private static Option<bool> _jsonOption = null!;
    private static Option<bool> _verboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        _jsonOption = jsonOption;
        _verboseOption = verboseOption;

        var archiveCommand = new Command("archive", "BSA/BA2 archive operations");

        archiveCommand.AddCommand(CreateInfoCommand());
        archiveCommand.AddCommand(CreateListCommand());
        archiveCommand.AddCommand(CreateExtractCommand());
        archiveCommand.AddCommand(CreateCreateCommand());
        archiveCommand.AddCommand(CreateStatusCommand());
        archiveCommand.AddCommand(CreateAddFilesCommand());
        archiveCommand.AddCommand(CreateRemoveFilesCommand());
        archiveCommand.AddCommand(CreateReplaceFilesCommand());

        return archiveCommand;
    }

    private static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    private static Command CreateInfoCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");

        var cmd = new Command("info", "Get information about an archive")
        {
            archiveArg
        };

        cmd.SetHandler((archive, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = service.GetInfo(archive);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            fileName = result.Value!.FileName,
                            type = result.Value.Type,
                            version = result.Value.Version,
                            fileCount = result.Value.FileCount,
                            fileSize = result.Value.FileSize
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!).ToJson(true));
                }
            }
            else if (result.Success)
            {
                var info = result.Value!;
                Console.WriteLine($"Archive: {info.FileName}");
                Console.WriteLine($"Type: {info.Type}");
                Console.WriteLine($"Version: {info.Version}");
                Console.WriteLine($"Files: {info.FileCount}");
                Console.WriteLine($"Size: {FormatSize(info.FileSize)}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, archiveArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateListCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var filterOption = new Option<string?>(
            aliases: new[] { "--filter", "-f" },
            description: "Filter files (e.g., *.nif, textures/*)");
        var limitOption = new Option<int>(
            "--limit",
            getDefaultValue: () => 100,
            description: "Maximum files to list (0 = all)");

        var cmd = new Command("list", "List files in an archive")
        {
            archiveArg,
            filterOption,
            limitOption
        };

        cmd.SetHandler((archive, filter, limit, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            // Get total count first for display purposes
            var infoResult = service.GetInfo(archive);
            var totalCount = infoResult.Success ? infoResult.Value!.FileCount : 0;

            // Apply limit (0 = no limit)
            int? effectiveLimit = limit > 0 ? limit : null;
            var result = service.ListFiles(archive, filter, effectiveLimit);

            if (json)
            {
                if (result.Success)
                {
                    var files = result.Value!;
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            files = files.Select(f => new { path = f.Path, size = f.Size, compressed = f.IsCompressed }),
                            totalInArchive = totalCount,
                            showing = files.Count
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, suggestions: result.Suggestions).ToJson(true));
                    Environment.ExitCode = 1;
                }
            }
            else if (result.Success)
            {
                var files = result.Value!;
                Console.WriteLine($"Files ({files.Count}" + (totalCount > 0 ? $" of {totalCount}" : "") + "):");
                foreach (var file in files.OrderBy(f => f.Path))
                {
                    var sizeStr = file.Size > 0 ? $" ({FormatSize(file.Size)})" : "";
                    var compStr = file.IsCompressed ? " [compressed]" : "";
                    Console.WriteLine($"  {file.Path}{sizeStr}{compStr}");
                }
                if (limit > 0 && files.Count >= limit && totalCount > limit)
                    Console.WriteLine($"  ... use --limit 0 to show all files");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("Suggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, archiveArg, filterOption, limitOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateExtractCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory") { IsRequired = true };
        var filterOption = new Option<string?>(
            aliases: new[] { "--filter", "-f" },
            description: "Filter files to extract (e.g., *.nif, textures/*)");

        var cmd = new Command("extract", "Extract files from an archive")
        {
            archiveArg,
            outputOption,
            filterOption
        };

        cmd.SetHandler(async (archive, output, filter, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.ExtractAsync(archive, output, filter);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            extractedCount = result.Value!.ExtractedCount,
                            outputDirectory = result.Value.OutputDirectory,
                            errors = result.Value.Errors
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!).ToJson(true));
                }
            }
            else if (result.Success)
            {
                var msg = result.Value!.ExtractedCount >= 0
                    ? $"Extracted {result.Value.ExtractedCount} file(s) to: {result.Value.OutputDirectory}"
                    : $"Extracted files to: {result.Value.OutputDirectory}";
                Console.WriteLine(msg);

                if (result.Value.Errors.Count > 0)
                {
                    Console.WriteLine("\nErrors:");
                    foreach (var err in result.Value.Errors)
                        Console.WriteLine($"  - {err}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, archiveArg, outputOption, filterOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateCreateCommand()
    {
        var dirArg = new Argument<string>("directory", "Source directory to archive");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output archive path (.bsa)") { IsRequired = true };
        var compressOption = new Option<bool>(
            "--compress",
            getDefaultValue: () => true,
            description: "Compress archive contents");
        var gameOption = new Option<string>(
            "--game",
            getDefaultValue: () => "sse",
            description: "Game type: sse, le, fo4, fo76");

        var cmd = new Command("create", "Create a new BSA archive from a directory")
        {
            dirArg,
            outputOption,
            compressOption,
            gameOption
        };

        cmd.SetHandler(async (dir, output, compress, game, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var gameType = game.ToLowerInvariant() switch
            {
                "le" or "skyrimle" => GameType.SkyrimLE,
                "fo4" or "fallout4" => GameType.Fallout4,
                "fo76" or "fallout76" => GameType.Fallout76,
                _ => GameType.SkyrimSE
            };

            var options = new ArchiveCreateOptions { Compress = compress, GameType = gameType };
            var result = await service.CreateAsync(dir, output, options);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            outputPath = result.Value
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, suggestions: result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Created archive: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nSuggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, dirArg, outputOption, compressOption, gameOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateStatusCommand()
    {
        var cmd = new Command("status", "Check if BSArch tool is available");

        cmd.SetHandler(async (json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.CheckToolsAsync();

            if (json)
            {
                Console.WriteLine(new
                {
                    success = result.Success,
                    bsarchPath = result.Value,
                    error = result.Error,
                    suggestions = result.Suggestions
                }.ToJson());
            }
            else if (result.Success)
            {
                Console.WriteLine($"BSArch available: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"BSArch not available: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nTo install:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddFilesCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var filesOption = new Option<string[]>(
            "--files",
            description: "Files to add to the archive") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
        var preserveOption = new Option<bool>(
            "--preserve-compression",
            getDefaultValue: () => true,
            description: "Preserve archive compression settings");

        var cmd = new Command("add-files", "Add files to an existing archive")
        {
            archiveArg,
            filesOption,
            preserveOption
        };

        cmd.SetHandler(async (archive, files, preserve, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.AddFilesAsync(archive, files.ToList(), preserve);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            filesAdded = result.Value!.FilesModified,
                            totalFiles = result.Value.TotalFiles,
                            errors = result.Value.Errors
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, suggestions: result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Added {result.Value!.FilesModified} file(s) to archive");
                Console.WriteLine($"Total files in archive: {result.Value.TotalFiles}");

                if (result.Value.Errors.Count > 0)
                {
                    Console.WriteLine("\nErrors:");
                    foreach (var err in result.Value.Errors)
                        Console.WriteLine($"  - {err}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nSuggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, archiveArg, filesOption, preserveOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateRemoveFilesCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var filterOption = new Option<string>(
            "--filter",
            description: "Filter pattern for files to remove (e.g., *.esp, scripts/*)") { IsRequired = true };
        var preserveOption = new Option<bool>(
            "--preserve-compression",
            getDefaultValue: () => true,
            description: "Preserve archive compression settings");

        var cmd = new Command("remove-files", "Remove files from an existing archive")
        {
            archiveArg,
            filterOption,
            preserveOption
        };

        cmd.SetHandler(async (archive, filter, preserve, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.RemoveFilesAsync(archive, filter, preserve);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            filesRemoved = result.Value!.FilesModified,
                            remainingFiles = result.Value.TotalFiles,
                            errors = result.Value.Errors
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, suggestions: result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Removed {result.Value!.FilesModified} file(s) from archive");
                Console.WriteLine($"Remaining files in archive: {result.Value.TotalFiles}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nSuggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, archiveArg, filterOption, preserveOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateReplaceFilesCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var sourceOption = new Option<string>(
            "--source",
            description: "Source directory containing replacement files") { IsRequired = true };
        var filterOption = new Option<string?>(
            "--filter",
            description: "Filter pattern for files to replace (e.g., *.pex, scripts/*)");
        var preserveOption = new Option<bool>(
            "--preserve-compression",
            getDefaultValue: () => true,
            description: "Preserve archive compression settings");

        var cmd = new Command("replace-files", "Replace files in an existing archive")
        {
            archiveArg,
            sourceOption,
            filterOption,
            preserveOption
        };

        cmd.SetHandler(async (archive, source, filter, preserve, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.ReplaceFilesAsync(archive, source, filter, preserve);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            filesReplaced = result.Value!.FilesModified,
                            totalFiles = result.Value.TotalFiles,
                            errors = result.Value.Errors
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, suggestions: result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Replaced {result.Value!.FilesModified} file(s) in archive");
                Console.WriteLine($"Total files in archive: {result.Value.TotalFiles}");

                if (result.Value.Errors.Count > 0)
                {
                    Console.WriteLine("\nErrors:");
                    foreach (var err in result.Value.Errors)
                        Console.WriteLine($"  - {err}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nSuggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, archiveArg, sourceOption, filterOption, preserveOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
