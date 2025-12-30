using System.CommandLine;
using System.Text.Json;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Papyrus.Services;
using SpookysAutomod.Papyrus.Templates;

namespace SpookysAutomod.Cli.Commands;

public static class PapyrusCommands
{
    private static Option<bool> _jsonOption = null!;
    private static Option<bool> _verboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        _jsonOption = jsonOption;
        _verboseOption = verboseOption;

        var papyrusCommand = new Command("papyrus", "Papyrus script compilation and decompilation");

        papyrusCommand.AddCommand(CreateDownloadCommand());
        papyrusCommand.AddCommand(CreateStatusCommand());
        papyrusCommand.AddCommand(CreateCompileCommand());
        papyrusCommand.AddCommand(CreateDecompileCommand());
        papyrusCommand.AddCommand(CreateValidateCommand());
        papyrusCommand.AddCommand(CreateGenerateCommand());

        return papyrusCommand;
    }

    private static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    private static Command CreateDownloadCommand()
    {
        var cmd = new Command("download", "Download Papyrus compiler and decompiler tools");

        cmd.SetHandler(async (json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PapyrusService(logger);

            logger.Info("Downloading Papyrus tools...");
            var result = await service.DownloadToolsAsync();

            if (json)
            {
                Console.WriteLine(result.ToJson(true));
            }
            else if (result.Success)
            {
                Console.WriteLine("Tools downloaded successfully!");
                var status = service.GetToolStatus();
                Console.WriteLine($"  Compiler: {(status.CompilerAvailable ? "Available" : "Not found")}");
                Console.WriteLine($"  Decompiler: {(status.DecompilerAvailable ? "Available" : "Not found")}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateStatusCommand()
    {
        var cmd = new Command("status", "Check status of Papyrus tools");

        cmd.SetHandler((json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PapyrusService(logger);
            var status = service.GetToolStatus();

            if (json)
            {
                Console.WriteLine(new
                {
                    success = true,
                    result = new
                    {
                        compilerAvailable = status.CompilerAvailable,
                        compilerPath = status.CompilerPath,
                        decompilerAvailable = status.DecompilerAvailable,
                        decompilerPath = status.DecompilerPath
                    }
                }.ToJson());
            }
            else
            {
                Console.WriteLine("Papyrus Tools Status:");
                Console.WriteLine($"  Compiler: {(status.CompilerAvailable ? "Available" : "Not installed")}");
                if (status.CompilerAvailable)
                    Console.WriteLine($"    Path: {status.CompilerPath}");
                Console.WriteLine($"  Decompiler: {(status.DecompilerAvailable ? "Available" : "Not installed")}");
                if (status.DecompilerAvailable)
                    Console.WriteLine($"    Path: {status.DecompilerPath}");

                if (!status.CompilerAvailable || !status.DecompilerAvailable)
                {
                    Console.WriteLine("\nRun 'spookys-automod papyrus download' to install missing tools.");
                }
            }
        }, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateCompileCommand()
    {
        var sourceArg = new Argument<string>("source", "Path to PSC file or directory");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for PEX files") { IsRequired = true };
        var headersOption = new Option<string>(
            aliases: new[] { "--headers", "-i" },
            description: "Directory containing script headers (e.g., Skyrim Scripts/Source)") { IsRequired = true };
        var optimizeOption = new Option<bool>(
            aliases: new[] { "--optimize", "-O" },
            getDefaultValue: () => true,
            description: "Enable optimization");

        var cmd = new Command("compile", "Compile Papyrus source files to PEX")
        {
            sourceArg,
            outputOption,
            headersOption,
            optimizeOption
        };

        cmd.SetHandler(async (source, output, headers, optimize, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PapyrusService(logger);

            var result = await service.CompileAsync(source, output, headers, optimize);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            compiledCount = result.Value!.CompiledCount,
                            outputDirectory = result.Value.OutputDirectory,
                            output = result.Value.Output
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, result.ErrorContext, result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Compilation successful!");
                Console.WriteLine($"  Compiled: {result.Value!.CompiledCount} file(s)");
                Console.WriteLine($"  Output: {result.Value.OutputDirectory}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (!string.IsNullOrEmpty(result.ErrorContext))
                    Console.Error.WriteLine($"\n{result.ErrorContext}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("\nSuggestions:");
                    foreach (var s in result.Suggestions)
                        Console.Error.WriteLine($"  - {s}");
                }
                Environment.ExitCode = 1;
            }
        }, sourceArg, outputOption, headersOption, optimizeOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateDecompileCommand()
    {
        var pexArg = new Argument<string>("pex", "Path to PEX file or directory");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for PSC files") { IsRequired = true };

        var cmd = new Command("decompile", "Decompile PEX files to Papyrus source")
        {
            pexArg,
            outputOption
        };

        cmd.SetHandler(async (pex, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PapyrusService(logger);

            var result = await service.DecompileAsync(pex, output);

            if (json)
            {
                if (result.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            decompiledCount = result.Value!.DecompiledCount,
                            outputPath = result.Value.OutputPath,
                            errors = result.Value.Errors
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(Result.Fail(result.Error!, result.ErrorContext, result.Suggestions).ToJson(true));
                }
            }
            else if (result.Success)
            {
                Console.WriteLine($"Decompilation successful!");
                if (result.Value!.DecompiledCount > 0)
                    Console.WriteLine($"  Decompiled: {result.Value.DecompiledCount} file(s)");
                Console.WriteLine($"  Output: {result.Value.OutputPath}");
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
        }, pexArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateValidateCommand()
    {
        var pscArg = new Argument<string>("psc", "Path to PSC file to validate");

        var cmd = new Command("validate", "Validate Papyrus source file syntax")
        {
            pscArg
        };

        cmd.SetHandler((psc, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PapyrusService(logger);

            var result = service.ValidateScript(psc);

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
                    Console.WriteLine($"Validation passed: {psc}");
                    if (validation.Warnings.Count > 0)
                    {
                        Console.WriteLine("\nWarnings:");
                        foreach (var w in validation.Warnings)
                            Console.WriteLine($"  - {w}");
                    }
                }
                else
                {
                    Console.WriteLine($"Validation failed: {psc}");
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
        }, pscArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateGenerateCommand()
    {
        var nameOption = new Option<string>("--name", "Script name (without extension)") { IsRequired = true };
        var extendsOption = new Option<string>(
            "--extends",
            getDefaultValue: () => "Quest",
            description: "Base type to extend (Quest, Actor, ObjectReference, MagicEffect, etc.)");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => ".",
            description: "Output directory");
        var descOption = new Option<string?>("--description", "Description comment for the script");

        var cmd = new Command("generate", "Generate a Papyrus script template")
        {
            nameOption,
            extendsOption,
            outputOption,
            descOption
        };

        cmd.SetHandler((name, extends_, output, desc, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var generator = new ScriptTemplateGenerator();

            var options = new ScriptTemplateOptions
            {
                Description = desc
            };

            var script = generator.Generate(name, extends_, options);
            var outputPath = Path.Combine(output, $"{name}.psc");

            Directory.CreateDirectory(output);
            File.WriteAllText(outputPath, script);

            if (json)
            {
                Console.WriteLine(new
                {
                    success = true,
                    result = new
                    {
                        scriptName = name,
                        extends_ = extends_,
                        outputPath = outputPath,
                        content = script
                    }
                }.ToJson());
            }
            else
            {
                Console.WriteLine($"Generated script: {outputPath}");
                Console.WriteLine($"  Extends: {extends_}");
            }
        }, nameOption, extendsOption, outputOption, descOption, _jsonOption, _verboseOption);

        return cmd;
    }
}
