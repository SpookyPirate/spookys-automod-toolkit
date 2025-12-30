using System.CommandLine;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Nif.Services;

namespace SpookysAutomod.Cli.Commands;

public static class NifCommands
{
    private static Option<bool> _jsonOption = null!;
    private static Option<bool> _verboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        _jsonOption = jsonOption;
        _verboseOption = verboseOption;

        var nifCommand = new Command("nif", "NIF mesh file operations");

        nifCommand.AddCommand(CreateInfoCommand());
        nifCommand.AddCommand(CreateTexturesCommand());
        nifCommand.AddCommand(CreateScaleCommand());
        nifCommand.AddCommand(CreateCopyCommand());

        return nifCommand;
    }

    private static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    private static Command CreateInfoCommand()
    {
        var nifArg = new Argument<string>("nif", "Path to the NIF file");

        var cmd = new Command("info", "Get information about a NIF file")
        {
            nifArg
        };

        cmd.SetHandler((nif, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new NifService(logger);

            var result = service.GetInfo(nif);

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
                            fileSize = result.Value.FileSize,
                            version = result.Value.Version,
                            headerString = result.Value.HeaderString
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
                Console.WriteLine($"NIF File: {info.FileName}");
                Console.WriteLine($"Size: {info.FileSize:N0} bytes");
                Console.WriteLine($"Header: {info.HeaderString}");
                if (!string.IsNullOrEmpty(info.Version))
                    Console.WriteLine($"Version: {info.Version}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, nifArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateTexturesCommand()
    {
        var nifArg = new Argument<string>("nif", "Path to the NIF file");

        var cmd = new Command("textures", "List textures referenced in a NIF file")
        {
            nifArg
        };

        cmd.SetHandler((nif, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new NifService(logger);

            var result = service.ListTextures(nif);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            textures = result.Value
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
                var textures = result.Value!;
                if (textures.Count == 0)
                {
                    Console.WriteLine("No textures found in NIF");
                }
                else
                {
                    Console.WriteLine($"Textures ({textures.Count}):");
                    foreach (var tex in textures.OrderBy(t => t))
                    {
                        Console.WriteLine($"  {tex}");
                    }
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, nifArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateScaleCommand()
    {
        var nifArg = new Argument<string>("nif", "Path to the NIF file");
        var factorArg = new Argument<float>("factor", "Scale factor (e.g., 1.5 for 150%)");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path (defaults to overwriting input)");

        var cmd = new Command("scale", "Scale a NIF mesh uniformly")
        {
            nifArg,
            factorArg,
            outputOption
        };

        cmd.SetHandler((nif, factor, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new NifService(logger);

            var outputPath = output ?? nif;
            var result = service.Scale(nif, factor, outputPath);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            outputPath = result.Value,
                            scaleFactor = factor
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
                Console.WriteLine($"Scaled NIF by {factor}x: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, nifArg, factorArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateCopyCommand()
    {
        var nifArg = new Argument<string>("nif", "Path to the source NIF file");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path") { IsRequired = true };

        var cmd = new Command("copy", "Copy a NIF file (validates format)")
        {
            nifArg,
            outputOption
        };

        cmd.SetHandler((nif, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new NifService(logger);

            var result = service.Copy(nif, output);

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
                    Console.WriteLine(Result.Fail(result.Error!).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Copied NIF to: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, nifArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }
}
