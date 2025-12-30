using System.CommandLine;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Mcm.Services;

namespace SpookysAutomod.Cli.Commands;

public static class McmCommands
{
    private static Option<bool> _jsonOption = null!;
    private static Option<bool> _verboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        _jsonOption = jsonOption;
        _verboseOption = verboseOption;

        var mcmCommand = new Command("mcm", "MCM Helper configuration operations");

        mcmCommand.AddCommand(CreateCreateCommand());
        mcmCommand.AddCommand(CreateInfoCommand());
        mcmCommand.AddCommand(CreateValidateCommand());
        mcmCommand.AddCommand(CreateAddToggleCommand());
        mcmCommand.AddCommand(CreateAddSliderCommand());

        return mcmCommand;
    }

    private static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    private static Command CreateCreateCommand()
    {
        var modNameArg = new Argument<string>("modName", "Internal mod name (no spaces)");
        var displayNameArg = new Argument<string>("displayName", "Display name shown in MCM");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => "./config.json",
            description: "Output file path");

        var cmd = new Command("create", "Create a new MCM configuration file")
        {
            modNameArg,
            displayNameArg,
            outputOption
        };

        cmd.SetHandler((modName, displayName, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new McmService(logger);

            var result = service.Create(modName, displayName, output);

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
                Console.WriteLine($"Created MCM config: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, modNameArg, displayNameArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateInfoCommand()
    {
        var configArg = new Argument<string>("config", "Path to the MCM config file");

        var cmd = new Command("info", "Get information about an MCM config")
        {
            configArg
        };

        cmd.SetHandler((config, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new McmService(logger);

            var result = service.GetInfo(config);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            modName = result.Value!.ModName,
                            displayName = result.Value.DisplayName,
                            minMcmVersion = result.Value.MinMcmVersion,
                            pageCount = result.Value.PageCount,
                            controlCount = result.Value.ControlCount,
                            pages = result.Value.Pages.Select(p => new
                            {
                                name = p.Name,
                                controlCount = p.ControlCount
                            })
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
                Console.WriteLine($"MCM Config: {info.DisplayName}");
                Console.WriteLine($"Internal Name: {info.ModName}");
                Console.WriteLine($"Min MCM Version: {info.MinMcmVersion}");
                Console.WriteLine($"Pages: {info.PageCount}");
                Console.WriteLine($"Controls: {info.ControlCount}");

                if (info.Pages.Count > 0 && verbose)
                {
                    Console.WriteLine("\nPages:");
                    foreach (var page in info.Pages)
                    {
                        Console.WriteLine($"  - {page.Name} ({page.ControlCount} controls)");
                    }
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, configArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateValidateCommand()
    {
        var configArg = new Argument<string>("config", "Path to the MCM config file");

        var cmd = new Command("validate", "Validate an MCM config file")
        {
            configArg
        };

        cmd.SetHandler((config, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new McmService(logger);

            var result = service.Validate(config);

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
                            errors = result.Value.Errors,
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
                    Console.WriteLine($"Validation passed: {config}");
                    if (validation.Warnings.Count > 0)
                    {
                        Console.WriteLine("\nWarnings:");
                        foreach (var w in validation.Warnings)
                            Console.WriteLine($"  - {w}");
                    }
                }
                else
                {
                    Console.WriteLine($"Validation failed: {config}");
                    Console.WriteLine("\nErrors:");
                    foreach (var e in validation.Errors)
                        Console.WriteLine($"  - {e}");
                    if (validation.Warnings.Count > 0)
                    {
                        Console.WriteLine("\nWarnings:");
                        foreach (var w in validation.Warnings)
                            Console.WriteLine($"  - {w}");
                    }
                    Environment.ExitCode = 1;
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, configArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddToggleCommand()
    {
        var configArg = new Argument<string>("config", "Path to the MCM config file");
        var idArg = new Argument<string>("id", "Control identifier");
        var textArg = new Argument<string>("text", "Display text");
        var helpOption = new Option<string?>("--help-text", "Help text shown on hover");
        var pageOption = new Option<string?>("--page", "Target page name");

        var cmd = new Command("add-toggle", "Add a toggle control to MCM config")
        {
            configArg,
            idArg,
            textArg,
            helpOption,
            pageOption
        };

        cmd.SetHandler((config, id, text, help, page, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new McmService(logger);

            var loadResult = service.Load(config);
            if (!loadResult.Success)
            {
                OutputError(loadResult.Error!, json);
                return;
            }

            var mcmConfig = loadResult.Value!;
            var addResult = service.AddToggle(mcmConfig, id, text, help, page);
            if (!addResult.Success)
            {
                OutputError(addResult.Error!, json);
                return;
            }

            var saveResult = service.Save(mcmConfig, config);

            if (json)
            {
                Console.WriteLine(new { success = true, result = new { id, text } }.ToJson());
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Added toggle: {id}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, configArg, idArg, textArg, helpOption, pageOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddSliderCommand()
    {
        var configArg = new Argument<string>("config", "Path to the MCM config file");
        var idArg = new Argument<string>("id", "Control identifier");
        var textArg = new Argument<string>("text", "Display text");
        var minOption = new Option<float>("--min", () => 0, "Minimum value");
        var maxOption = new Option<float>("--max", () => 100, "Maximum value");
        var stepOption = new Option<float>("--step", () => 1, "Step increment");

        var cmd = new Command("add-slider", "Add a slider control to MCM config")
        {
            configArg,
            idArg,
            textArg,
            minOption,
            maxOption,
            stepOption
        };

        cmd.SetHandler((config, id, text, min, max, step, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new McmService(logger);

            var loadResult = service.Load(config);
            if (!loadResult.Success)
            {
                OutputError(loadResult.Error!, json);
                return;
            }

            var mcmConfig = loadResult.Value!;
            var addResult = service.AddSlider(mcmConfig, id, text, min, max, step, null, null);
            if (!addResult.Success)
            {
                OutputError(addResult.Error!, json);
                return;
            }

            var saveResult = service.Save(mcmConfig, config);

            if (json)
            {
                Console.WriteLine(new { success = true, result = new { id, text, min, max, step } }.ToJson());
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Added slider: {id} ({min} - {max})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, configArg, idArg, textArg, minOption, maxOption, stepOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static void OutputError(string error, bool json)
    {
        if (json)
        {
            Console.WriteLine(Result.Fail(error).ToJson(true));
        }
        else
        {
            Console.Error.WriteLine($"Error: {error}");
        }
        Environment.ExitCode = 1;
    }
}
