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
        archiveCommand.AddCommand(CreateUpdateFileCommand());
        archiveCommand.AddCommand(CreateExtractFileCommand());
        archiveCommand.AddCommand(CreateMergeCommand());
        archiveCommand.AddCommand(CreateValidateCommand());
        archiveCommand.AddCommand(CreateOptimizeCommand());
        archiveCommand.AddCommand(CreateDiffCommand());

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
        var baseDirOption = new Option<string?>(
            "--base-dir",
            description: "Base directory for calculating relative paths (auto-detected if not specified)");
        var preserveOption = new Option<bool>(
            "--preserve-compression",
            getDefaultValue: () => true,
            description: "Preserve archive compression settings");

        var cmd = new Command("add-files", "Add files to an existing archive")
        {
            archiveArg,
            filesOption,
            baseDirOption,
            preserveOption
        };

        cmd.SetHandler(async (archive, files, baseDir, preserve, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.AddFilesAsync(archive, files.ToList(), baseDir, preserve);

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
        }, archiveArg, filesOption, baseDirOption, preserveOption, _jsonOption, _verboseOption);

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

    private static Command CreateDiffCommand()
    {
        var archive1Arg = new Argument<string>("archive1", "First archive to compare");
        var archive2Arg = new Argument<string>("archive2", "Second archive to compare");

        var cmd = new Command("diff", "Compare two archive versions")
        {
            archive1Arg,
            archive2Arg
        };

        cmd.SetHandler((archive1, archive2, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = service.DiffArchives(archive1, archive2);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            archive1 = result.Value!.Archive1,
                            archive2 = result.Value.Archive2,
                            filesAdded = result.Value.FilesAdded,
                            filesRemoved = result.Value.FilesRemoved,
                            filesModified = result.Value.FilesModified,
                            filesUnchanged = result.Value.FilesUnchanged.Count,
                            totalFiles1 = result.Value.TotalFiles1,
                            totalFiles2 = result.Value.TotalFiles2
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
                var diff = result.Value!;
                Console.WriteLine($"Comparing: {diff.Archive1} vs {diff.Archive2}");
                Console.WriteLine();

                if (diff.FilesAdded.Count > 0)
                {
                    Console.WriteLine($"Added ({diff.FilesAdded.Count}):");
                    foreach (var file in diff.FilesAdded.Take(10))
                        Console.WriteLine($"  + {file}");
                    if (diff.FilesAdded.Count > 10)
                        Console.WriteLine($"  ... and {diff.FilesAdded.Count - 10} more");
                    Console.WriteLine();
                }

                if (diff.FilesRemoved.Count > 0)
                {
                    Console.WriteLine($"Removed ({diff.FilesRemoved.Count}):");
                    foreach (var file in diff.FilesRemoved.Take(10))
                        Console.WriteLine($"  - {file}");
                    if (diff.FilesRemoved.Count > 10)
                        Console.WriteLine($"  ... and {diff.FilesRemoved.Count - 10} more");
                    Console.WriteLine();
                }

                if (diff.FilesModified.Count > 0)
                {
                    Console.WriteLine($"Modified ({diff.FilesModified.Count}):");
                    foreach (var file in diff.FilesModified.Take(10))
                        Console.WriteLine($"  * {file}");
                    if (diff.FilesModified.Count > 10)
                        Console.WriteLine($"  ... and {diff.FilesModified.Count - 10} more");
                    Console.WriteLine();
                }

                Console.WriteLine($"Unchanged: {diff.FilesUnchanged.Count}");
                Console.WriteLine();
                Console.WriteLine($"Summary:");
                Console.WriteLine($"  {diff.Archive1}: {diff.TotalFiles1} files");
                Console.WriteLine($"  {diff.Archive2}: {diff.TotalFiles2} files");
                Console.WriteLine($"  Net change: {diff.TotalFiles2 - diff.TotalFiles1:+#;-#;0} files");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, archive1Arg, archive2Arg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateOptimizeCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var outputOption = new Option<string?>(
            "--output",
            description: "Output path (defaults to overwriting original)");
        var compressOption = new Option<bool>(
            "--compress",
            getDefaultValue: () => true,
            description: "Enable compression");

        var cmd = new Command("optimize", "Optimize archive by repacking with compression")
        {
            archiveArg,
            outputOption,
            compressOption
        };

        cmd.SetHandler(async (archive, output, compress, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var options = new ArchiveCreateOptions { Compress = compress };
            var result = await service.OptimizeAsync(archive, output, options);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            outputPath = result.Value!.OutputPath,
                            originalSize = result.Value.OriginalSize,
                            optimizedSize = result.Value.OptimizedSize,
                            savings = result.Value.Savings,
                            savingsPercent = result.Value.SavingsPercent,
                            fileCount = result.Value.FileCount
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
                var opt = result.Value!;
                Console.WriteLine($"Optimized: {opt.OutputPath}");
                Console.WriteLine($"Original size: {FormatSize(opt.OriginalSize)}");
                Console.WriteLine($"Optimized size: {FormatSize(opt.OptimizedSize)}");

                if (opt.Savings > 0)
                {
                    Console.WriteLine($"Savings: {FormatSize(opt.Savings)} ({opt.SavingsPercent:F1}%)");
                }
                else if (opt.Savings < 0)
                {
                    Console.WriteLine($"Size increased: {FormatSize(-opt.Savings)} ({-opt.SavingsPercent:F1}%)");
                }
                else
                {
                    Console.WriteLine("Size unchanged");
                }

                Console.WriteLine($"Files: {opt.FileCount}");
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
        }, archiveArg, outputOption, compressOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateValidateCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");

        var cmd = new Command("validate", "Check archive integrity")
        {
            archiveArg
        };

        cmd.SetHandler((archive, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = service.Validate(archive);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            isValid = result.Value!.IsValid,
                            fileCount = result.Value.FileCount,
                            archiveSize = result.Value.ArchiveSize,
                            archiveType = result.Value.ArchiveType,
                            issues = result.Value.Issues,
                            warnings = result.Value.Warnings
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
                var validation = result.Value!;

                if (validation.IsValid)
                {
                    Console.WriteLine($"✓ Archive is valid");
                    Console.WriteLine($"  Type: {validation.ArchiveType}");
                    Console.WriteLine($"  Files: {validation.FileCount}");
                    Console.WriteLine($"  Size: {FormatSize(validation.ArchiveSize)}");
                }
                else
                {
                    Console.WriteLine($"✗ Archive has issues");
                }

                if (validation.Issues.Count > 0)
                {
                    Console.WriteLine($"\nIssues ({validation.Issues.Count}):");
                    foreach (var issue in validation.Issues)
                        Console.WriteLine($"  ✗ {issue}");
                }

                if (validation.Warnings.Count > 0)
                {
                    Console.WriteLine($"\nWarnings ({validation.Warnings.Count}):");
                    foreach (var warning in validation.Warnings.Take(10))
                        Console.WriteLine($"  ⚠ {warning}");
                    if (validation.Warnings.Count > 10)
                        Console.WriteLine($"  ... and {validation.Warnings.Count - 10} more");
                }

                Environment.ExitCode = validation.IsValid ? 0 : 1;
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, archiveArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateMergeCommand()
    {
        var archivesArg = new Argument<string[]>("archives", "Archives to merge (later ones overwrite earlier)") { Arity = ArgumentArity.OneOrMore };
        var outputOption = new Option<string>(
            "--output",
            description: "Output merged archive path") { IsRequired = true };
        var compressOption = new Option<bool>(
            "--compress",
            getDefaultValue: () => true,
            description: "Compress merged archive");

        var cmd = new Command("merge", "Merge multiple archives into one")
        {
            archivesArg,
            outputOption,
            compressOption
        };

        cmd.SetHandler(async (archives, output, compress, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var options = new ArchiveCreateOptions { Compress = compress };
            var result = await service.MergeArchivesAsync(archives.ToList(), output, options);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            outputPath = result.Value!.OutputPath,
                            archivesMerged = result.Value.ArchivesMerged,
                            totalFiles = result.Value.TotalFiles,
                            conflicts = result.Value.Conflicts,
                            filesPerArchive = result.Value.FilesPerArchive
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
                Console.WriteLine($"Merged {result.Value!.ArchivesMerged} archives");
                Console.WriteLine($"Output: {result.Value.OutputPath}");
                Console.WriteLine($"Total files: {result.Value.TotalFiles}");

                if (result.Value.Conflicts.Count > 0)
                {
                    Console.WriteLine($"\nConflicts resolved ({result.Value.Conflicts.Count}):");
                    foreach (var conflict in result.Value.Conflicts.Take(10))
                        Console.WriteLine($"  - {conflict}");
                    if (result.Value.Conflicts.Count > 10)
                        Console.WriteLine($"  ... and {result.Value.Conflicts.Count - 10} more");
                }

                Console.WriteLine("\nFiles per archive:");
                foreach (var kvp in result.Value.FilesPerArchive)
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value} files");
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
        }, archivesArg, outputOption, compressOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateExtractFileCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var fileOption = new Option<string>(
            "--file",
            description: "File path in archive to extract (e.g., scripts/MyScript.pex)") { IsRequired = true };
        var outputOption = new Option<string>(
            "--output",
            description: "Output file path") { IsRequired = true };

        var cmd = new Command("extract-file", "Extract a single file from an archive")
        {
            archiveArg,
            fileOption,
            outputOption
        };

        cmd.SetHandler(async (archive, file, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.ExtractFileAsync(archive, file, output);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            file = file,
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
                Console.WriteLine($"Extracted: {file}");
                Console.WriteLine($"To: {result.Value}");
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
        }, archiveArg, fileOption, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateUpdateFileCommand()
    {
        var archiveArg = new Argument<string>("archive", "Path to the BSA/BA2 archive");
        var fileOption = new Option<string>(
            "--file",
            description: "Target file path in archive (e.g., scripts/MyScript.pex)") { IsRequired = true };
        var sourceOption = new Option<string>(
            "--source",
            description: "Source file to update with") { IsRequired = true };
        var preserveOption = new Option<bool>(
            "--preserve-compression",
            getDefaultValue: () => true,
            description: "Preserve archive compression settings");

        var cmd = new Command("update-file", "Update a single file in an existing archive")
        {
            archiveArg,
            fileOption,
            sourceOption,
            preserveOption
        };

        cmd.SetHandler(async (archive, file, source, preserve, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = await service.UpdateFileAsync(archive, file, source, preserve);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            fileUpdated = file,
                            totalFiles = result.Value!.TotalFiles,
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
                Console.WriteLine($"Updated file: {file}");
                Console.WriteLine($"Total files in archive: {result.Value!.TotalFiles}");

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
        }, archiveArg, fileOption, sourceOption, preserveOption, _jsonOption, _verboseOption);

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
