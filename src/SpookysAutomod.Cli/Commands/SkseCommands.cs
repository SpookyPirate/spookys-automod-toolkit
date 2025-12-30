using System.CommandLine;
using System.Text.Json;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Skse.Models;
using SpookysAutomod.Skse.Services;

namespace SpookysAutomod.Cli.Commands;

public static class SkseCommands
{
    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        var skseCommand = new Command("skse", "SKSE C++ plugin project management");

        skseCommand.AddCommand(CreateCreateCommand(jsonOption, verboseOption));
        skseCommand.AddCommand(CreateInfoCommand(jsonOption, verboseOption));
        skseCommand.AddCommand(CreateListTemplatesCommand(jsonOption));
        skseCommand.AddCommand(CreateAddFunctionCommand(jsonOption, verboseOption));

        return skseCommand;
    }

    private static Command CreateCreateCommand(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        var command = new Command("create", "Create a new SKSE plugin project");

        var nameArg = new Argument<string>("name", "Project name");
        var templateOpt = new Option<string>("--template", () => "basic", "Template to use (basic, papyrus-native)");
        var outputOpt = new Option<string>("--output", () => ".", "Output directory");
        var authorOpt = new Option<string>("--author", () => "Unknown", "Author name");
        var descriptionOpt = new Option<string>("--description", () => "", "Project description");

        command.AddArgument(nameArg);
        command.AddOption(templateOpt);
        command.AddOption(outputOpt);
        command.AddOption(authorOpt);
        command.AddOption(descriptionOpt);

        command.SetHandler((name, template, output, author, description, json, verbose) =>
        {
            var logger = new ConsoleLogger(verbose);
            var service = new SkseProjectService(logger);
            var config = new SkseProjectConfig
            {
                Name = name,
                Template = template,
                Author = author,
                Description = string.IsNullOrEmpty(description) ? $"{name} SKSE Plugin" : description
            };

            var result = service.CreateProject(config, output);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = result.Success,
                    result = result.Value,
                    error = result.Error,
                    suggestions = result.Suggestions
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                if (result.Success)
                {
                    Console.WriteLine($"Created SKSE project: {result.Value}");
                    Console.WriteLine();
                    Console.WriteLine("Next steps:");
                    Console.WriteLine($"  cd {result.Value}");
                    Console.WriteLine("  Run 'cmake -B build -S .' to configure");
                    Console.WriteLine("  Run 'cmake --build build --config Release' to build");
                }
                else
                {
                    Console.Error.WriteLine($"Error: {result.Error}");
                    if (result.Suggestions != null && result.Suggestions.Count > 0)
                    {
                        Console.Error.WriteLine("Suggestions:");
                        foreach (var suggestion in result.Suggestions)
                        {
                            Console.Error.WriteLine($"  - {suggestion}");
                        }
                    }
                }
            }
        }, nameArg, templateOpt, outputOpt, authorOpt, descriptionOpt, jsonOption, verboseOption);

        return command;
    }

    private static Command CreateInfoCommand(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        var command = new Command("info", "Get information about an SKSE project");

        var pathArg = new Argument<string>("path", () => ".", "Project directory");

        command.AddArgument(pathArg);

        command.SetHandler((path, json, verbose) =>
        {
            var logger = new ConsoleLogger(verbose);
            var service = new SkseProjectService(logger);
            var result = service.GetProjectInfo(path);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = result.Success,
                    result = result.Value,
                    error = result.Error,
                    suggestions = result.Suggestions
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                if (result.Success && result.Value != null)
                {
                    var config = result.Value;
                    Console.WriteLine($"Project: {config.Name}");
                    Console.WriteLine($"Author: {config.Author}");
                    Console.WriteLine($"Version: {config.Version}");
                    Console.WriteLine($"Template: {config.Template}");
                    Console.WriteLine($"Description: {config.Description}");
                    Console.WriteLine($"Target Versions: {string.Join(", ", config.TargetVersions)}");

                    if (config.PapyrusFunctions.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Papyrus Functions:");
                        foreach (var func in config.PapyrusFunctions)
                        {
                            var paramStr = string.Join(", ", func.Parameters.Select(p => $"{p.Type} {p.Name}"));
                            Console.WriteLine($"  {func.ReturnType} {func.Name}({paramStr})");
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Error: {result.Error}");
                }
            }
        }, pathArg, jsonOption, verboseOption);

        return command;
    }

    private static Command CreateListTemplatesCommand(Option<bool> jsonOption)
    {
        var command = new Command("templates", "List available SKSE templates");

        command.SetHandler((json) =>
        {
            var logger = new ConsoleLogger(false);
            var service = new SkseProjectService(logger);
            var result = service.ListTemplates();

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = result.Success,
                    result = result.Value,
                    error = result.Error
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine("Available SKSE Templates:");
                Console.WriteLine();
                foreach (var template in result.Value ?? Array.Empty<string>())
                {
                    Console.WriteLine($"  {template}");
                }
            }
        }, jsonOption);

        return command;
    }

    private static Command CreateAddFunctionCommand(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        var command = new Command("add-function", "Add a Papyrus native function to a project");

        var projectArg = new Argument<string>("project", () => ".", "Project directory");
        var nameOpt = new Option<string>("--name", "Function name") { IsRequired = true };
        var returnOpt = new Option<string>("--return", () => "void", "Return type");
        var paramsOpt = new Option<string[]>("--param", "Parameters (format: type:name)") { AllowMultipleArgumentsPerToken = true };

        command.AddArgument(projectArg);
        command.AddOption(nameOpt);
        command.AddOption(returnOpt);
        command.AddOption(paramsOpt);

        command.SetHandler((project, name, returnType, paramStrings, json, verbose) =>
        {
            var logger = new ConsoleLogger(verbose);
            var service = new SkseProjectService(logger);

            var function = new PapyrusNativeFunction
            {
                Name = name,
                ReturnType = returnType
            };

            if (paramStrings != null)
            {
                foreach (var paramStr in paramStrings)
                {
                    var parts = paramStr.Split(':');
                    if (parts.Length == 2)
                    {
                        function.Parameters.Add(new PapyrusParameter
                        {
                            Type = parts[0],
                            Name = parts[1]
                        });
                    }
                }
            }

            var result = service.AddPapyrusFunction(project, function);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = result.Success,
                    error = result.Error,
                    suggestions = result.Suggestions
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                if (result.Success)
                {
                    Console.WriteLine($"Added function: {name}");
                    Console.WriteLine("  Rebuild the project to include the new function");
                }
                else
                {
                    Console.Error.WriteLine($"Error: {result.Error}");
                    if (result.Suggestions != null && result.Suggestions.Count > 0)
                    {
                        foreach (var suggestion in result.Suggestions)
                        {
                            Console.Error.WriteLine($"  - {suggestion}");
                        }
                    }
                }
            }
        }, projectArg, nameOpt, returnOpt, paramsOpt, jsonOption, verboseOption);

        return command;
    }
}
