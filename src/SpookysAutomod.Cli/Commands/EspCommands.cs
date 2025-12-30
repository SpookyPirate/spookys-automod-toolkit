using System.CommandLine;
using System.Text.Json;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Esp.Builders;
using SpookysAutomod.Esp.Services;
using Mutagen.Bethesda.Skyrim;

namespace SpookysAutomod.Cli.Commands;

public static class EspCommands
{
    private static Option<bool> _jsonOption = null!;
    private static Option<bool> _verboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        _jsonOption = jsonOption;
        _verboseOption = verboseOption;

        var espCommand = new Command("esp", "ESP/ESL plugin operations");

        espCommand.AddCommand(CreateCreateCommand());
        espCommand.AddCommand(CreateInfoCommand());
        espCommand.AddCommand(CreateAddQuestCommand());
        espCommand.AddCommand(CreateAddSpellCommand());
        espCommand.AddCommand(CreateAddGlobalCommand());
        espCommand.AddCommand(CreateAttachScriptCommand());
        espCommand.AddCommand(CreateGenerateSeqCommand());

        return espCommand;
    }

    private static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    private static Command CreateCreateCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the plugin file (e.g., MyMod.esp)");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => ".",
            description: "Output directory");
        var lightOption = new Option<bool>("--light", "Create as a light plugin (ESL flagged)");
        var authorOption = new Option<string?>("--author", "Author name for the plugin header");
        var descOption = new Option<string?>("--description", "Description for the plugin header");

        var cmd = new Command("create", "Create a new ESP/ESL plugin")
        {
            nameArg,
            outputOption,
            lightOption,
            authorOption,
            descOption
        };

        cmd.SetHandler((name, output, light, author, desc, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.CreatePlugin(name, output, light, author, desc);

            if (json)
            {
                Console.WriteLine(result.ToJson(true));
            }
            else if (result.Success)
            {
                Console.WriteLine($"Created plugin: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, nameArg, outputOption, lightOption, authorOption, descOption,
           _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateInfoCommand()
    {
        var pathArg = new Argument<string>("plugin", "Path to the plugin file");

        var cmd = new Command("info", "Get information about a plugin")
        {
            pathArg
        };

        cmd.SetHandler((path, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.GetPluginInfo(path);

            if (json)
            {
                Console.WriteLine(Result<PluginInfo>.Ok(result.Value!).ToJson(true));
            }
            else if (result.Success && result.Value != null)
            {
                var info = result.Value;
                Console.WriteLine($"Plugin: {info.FileName}");
                Console.WriteLine($"Path: {info.FilePath}");
                Console.WriteLine($"Size: {info.FileSize:N0} bytes");
                Console.WriteLine($"Light: {info.IsLight}");
                Console.WriteLine($"Master: {info.IsMaster}");

                if (!string.IsNullOrEmpty(info.Author))
                    Console.WriteLine($"Author: {info.Author}");

                if (info.MasterFiles.Count > 0)
                {
                    Console.WriteLine($"Masters: {string.Join(", ", info.MasterFiles)}");
                }

                Console.WriteLine($"\nRecords ({info.TotalRecords} total):");
                foreach (var (type, count) in info.RecordCounts.Where(kv => kv.Value > 0))
                {
                    Console.WriteLine($"  {type}: {count}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, pathArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddQuestCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the quest");
        var nameOption = new Option<string?>("--name", "Display name for the quest");
        var startEnabledOption = new Option<bool>("--start-enabled", "Quest starts when game loads");
        var runOnceOption = new Option<bool>("--run-once", "Quest runs only once");
        var priorityOption = new Option<byte>("--priority", getDefaultValue: () => 50, description: "Quest priority (0-255)");

        var cmd = new Command("add-quest", "Add a quest record to a plugin")
        {
            pluginArg,
            editorIdArg,
            nameOption,
            startEnabledOption,
            runOnceOption,
            priorityOption
        };

        cmd.SetHandler((plugin, editorId, name, startEnabled, runOnce, priority, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;
            var builder = new QuestBuilder(mod, editorId);

            if (!string.IsNullOrEmpty(name))
                builder.WithName(name);

            if (startEnabled)
                builder.StartEnabled();

            if (runOnce)
                builder.RunOnce();

            builder.WithPriority(priority);

            var quest = builder.Build();
            var saveResult = service.SavePlugin(mod, plugin);

            if (json)
            {
                Console.WriteLine(new
                {
                    success = true,
                    result = new
                    {
                        editorId = quest.EditorID,
                        formId = quest.FormKey.ToString(),
                        name = quest.Name?.String
                    }
                }.ToJson());
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Added quest: {quest.EditorID} ({quest.FormKey})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, editorIdArg, nameOption, startEnabledOption, runOnceOption, priorityOption,
           _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddSpellCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the spell");
        var nameOption = new Option<string?>("--name", "Display name for the spell");
        var typeOption = new Option<string>("--type", getDefaultValue: () => "spell",
            description: "Spell type: spell, power, lesser-power, ability");
        var costOption = new Option<uint>("--cost", getDefaultValue: () => 0, description: "Base magicka cost");

        var cmd = new Command("add-spell", "Add a spell record to a plugin")
        {
            pluginArg,
            editorIdArg,
            nameOption,
            typeOption,
            costOption
        };

        cmd.SetHandler((plugin, editorId, name, type, cost, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;
            var builder = new SpellBuilder(mod, editorId);

            if (!string.IsNullOrEmpty(name))
                builder.WithName(name);

            builder.WithBaseCost(cost);

            switch (type.ToLowerInvariant())
            {
                case "power":
                    builder.AsGreaterPower();
                    break;
                case "lesser-power":
                    builder.AsLesserPower();
                    break;
                case "ability":
                    builder.AsAbility();
                    break;
                default:
                    builder.WithType(SpellType.Spell);
                    break;
            }

            var spell = builder.Build();
            var saveResult = service.SavePlugin(mod, plugin);

            if (json)
            {
                Console.WriteLine(new
                {
                    success = true,
                    result = new
                    {
                        editorId = spell.EditorID,
                        formId = spell.FormKey.ToString(),
                        name = spell.Name?.String
                    }
                }.ToJson());
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Added spell: {spell.EditorID} ({spell.FormKey})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, editorIdArg, nameOption, typeOption, costOption,
           _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddGlobalCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the global");
        var typeOption = new Option<string>("--type", getDefaultValue: () => "float",
            description: "Global type: short, long, float");
        var valueOption = new Option<float>("--value", getDefaultValue: () => 0, description: "Initial value");

        var cmd = new Command("add-global", "Add a global variable to a plugin")
        {
            pluginArg,
            editorIdArg,
            typeOption,
            valueOption
        };

        cmd.SetHandler((plugin, editorId, type, value, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;
            var builder = new GlobalBuilder(mod, editorId);

            switch (type.ToLowerInvariant())
            {
                case "short":
                    builder.AsShort((short)value);
                    break;
                case "long":
                    builder.AsLong((int)value);
                    break;
                default:
                    builder.AsFloat(value);
                    break;
            }

            var global = builder.Build();
            var saveResult = service.SavePlugin(mod, plugin);

            // Get the value from the concrete global type
            var globalValue = global switch
            {
                GlobalFloat f => f.Data,
                GlobalInt i => i.Data,
                GlobalShort s => s.Data,
                _ => 0f
            };

            if (json)
            {
                Console.WriteLine(new
                {
                    success = true,
                    result = new
                    {
                        editorId = global.EditorID,
                        formId = global.FormKey.ToString(),
                        value = globalValue
                    }
                }.ToJson());
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Added global: {global.EditorID} ({global.FormKey}) = {globalValue}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, editorIdArg, typeOption, valueOption,
           _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAttachScriptCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var questOption = new Option<string>("--quest", "Editor ID of the quest to attach to") { IsRequired = true };
        var scriptOption = new Option<string>("--script", "Name of the script to attach") { IsRequired = true };

        var cmd = new Command("attach-script", "Attach a script to a quest")
        {
            pluginArg,
            questOption,
            scriptOption
        };

        cmd.SetHandler((plugin, questId, scriptName, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;

            // Find the quest
            var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questId);
            if (quest == null)
            {
                OutputError($"Quest not found: {questId}", json,
                    suggestions: new[] { "Check the quest editor ID is correct", "Use 'esp info' to list quests in the plugin" });
                return;
            }

            // Get or create adapter
            var adapter = quest.VirtualMachineAdapter as QuestAdapter ?? new QuestAdapter();

            // Check if script already attached
            if (adapter.Scripts.Any(s => s.Name == scriptName))
            {
                OutputError($"Script already attached: {scriptName}", json);
                return;
            }

            // Add script
            var scriptEntry = new ScriptEntry
            {
                Name = scriptName,
                Flags = ScriptEntry.Flag.Local
            };
            adapter.Scripts.Add(scriptEntry);
            quest.VirtualMachineAdapter = adapter;

            var saveResult = service.SavePlugin(mod, plugin);

            if (json)
            {
                Console.WriteLine(new
                {
                    success = true,
                    result = new
                    {
                        quest = questId,
                        script = scriptName
                    }
                }.ToJson());
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Attached script '{scriptName}' to quest '{questId}'");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, questOption, scriptOption,
           _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateGenerateSeqCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => ".",
            description: "Output directory for SEQ file");

        var cmd = new Command("generate-seq", "Generate SEQ file for start-enabled quests")
        {
            pluginArg,
            outputOption
        };

        cmd.SetHandler((plugin, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.GenerateSeqFile(plugin, output);

            if (json)
            {
                Console.WriteLine(result.ToJson(true));
            }
            else if (result.Success)
            {
                Console.WriteLine($"Generated SEQ file: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                if (result.Suggestions?.Count > 0)
                {
                    Console.Error.WriteLine("Suggestions:");
                    foreach (var suggestion in result.Suggestions)
                        Console.Error.WriteLine($"  - {suggestion}");
                }
                Environment.ExitCode = 1;
            }
        }, pluginArg, outputOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static void OutputError(string error, bool json, IEnumerable<string>? suggestions = null)
    {
        if (json)
        {
            Console.WriteLine(Result.Fail(error, suggestions: suggestions?.ToList()).ToJson(true));
        }
        else
        {
            Console.Error.WriteLine($"Error: {error}");
            if (suggestions != null)
            {
                Console.Error.WriteLine("Suggestions:");
                foreach (var s in suggestions)
                    Console.Error.WriteLine($"  - {s}");
            }
        }
        Environment.ExitCode = 1;
    }
}

// Extension for anonymous type JSON
internal static class JsonExtensions
{
    public static string ToJson(this object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
