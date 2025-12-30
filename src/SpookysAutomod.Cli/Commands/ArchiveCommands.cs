using System.CommandLine;
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

        var cmd = new Command("list", "List files in an archive")
        {
            archiveArg,
            filterOption
        };

        cmd.SetHandler((archive, filter, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = service.ListFiles(archive, filter);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            files = result.Value,
                            count = result.Value!.Count
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
                var files = result.Value!;
                Console.WriteLine($"Files ({files.Count}):");
                foreach (var file in files.OrderBy(f => f))
                {
                    Console.WriteLine($"  {file}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, archiveArg, filterOption, _jsonOption, _verboseOption);

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

        cmd.SetHandler((archive, output, filter, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var result = service.Extract(archive, output, filter);

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
                Console.WriteLine($"Extracted {result.Value!.ExtractedCount} file(s) to: {result.Value.OutputDirectory}");
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
            description: "Output archive path (extension determines type: .bsa or .ba2)") { IsRequired = true };
        var compressOption = new Option<bool>(
            "--compress",
            getDefaultValue: () => true,
            description: "Compress archive contents");

        var cmd = new Command("create", "Create a new archive from a directory")
        {
            dirArg,
            outputOption,
            compressOption
        };

        cmd.SetHandler((dir, output, compress, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new ArchiveService(logger);

            var options = new ArchiveCreateOptions { Compress = compress };
            var result = service.Create(dir, output, options);

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
        }, dirArg, outputOption, compressOption, _jsonOption, _verboseOption);

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
