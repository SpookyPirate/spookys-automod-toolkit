using System.CommandLine;
using SpookysAutomod.Esp.Services;
using Mutagen.Bethesda.Skyrim;

namespace SpookysAutomod.Cli.Commands;

/// <summary>
/// Script operations: attach-script, set-property, add-alias, attach-alias-script, set-alias-property, generate-seq
/// </summary>
internal static class EspScriptCommands
{
    public static void Register(Command parent)
    {
        parent.AddCommand(CreateAttachScriptCommand());
        parent.AddCommand(CreateSetPropertyCommand());
        parent.AddCommand(CreateAddAliasCommand());
        parent.AddCommand(CreateAttachAliasScriptCommand());
        parent.AddCommand(CreateSetAliasPropertyCommand());
        parent.AddCommand(CreateGenerateSeqCommand());
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
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;

            // Find the quest
            var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questId);
            if (quest == null)
            {
                EspCommands.OutputError($"Quest not found: {questId}", json,
                    suggestions: new[] { "Check the quest editor ID is correct", "Use 'esp info' to list quests in the plugin" });
                return;
            }

            // Get or create adapter
            var adapter = quest.VirtualMachineAdapter as QuestAdapter ?? new QuestAdapter();

            // Check if script already attached
            if (adapter.Scripts.Any(s => s.Name == scriptName))
            {
                EspCommands.OutputError($"Script already attached: {scriptName}", json);
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
                if (saveResult.Success)
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
                else
                {
                    Console.WriteLine(new { success = false, error = saveResult.Error }.ToJson());
                    Environment.ExitCode = 1;
                }
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
           EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateSetPropertyCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var questOption = new Option<string>("--quest", "Editor ID of the quest") { IsRequired = true };
        var scriptOption = new Option<string>("--script", "Name of the script") { IsRequired = true };
        var propertyOption = new Option<string>("--property", "Property name") { IsRequired = true };
        var typeOption = new Option<string>("--type", "Property type: object, alias, int, float, bool, string") { IsRequired = true };
        var valueOption = new Option<string>("--value", "Property value (for object: 'Plugin.esp|0xFormID', for alias: alias name)") { IsRequired = true };
        var aliasTargetOption = new Option<string?>("--alias-target", "Target alias name (for setting properties on alias scripts instead of quest scripts)");

        var cmd = new Command("set-property", "Set a script property on a quest or alias script")
        {
            pluginArg, questOption, scriptOption, propertyOption, typeOption, valueOption, aliasTargetOption
        };

        cmd.SetHandler((context) =>
        {
            var plugin = context.ParseResult.GetValueForArgument(pluginArg);
            var questId = context.ParseResult.GetValueForOption(questOption)!;
            var scriptName = context.ParseResult.GetValueForOption(scriptOption)!;
            var propertyName = context.ParseResult.GetValueForOption(propertyOption)!;
            var propType = context.ParseResult.GetValueForOption(typeOption)!;
            var value = context.ParseResult.GetValueForOption(valueOption)!;
            var aliasTarget = context.ParseResult.GetValueForOption(aliasTargetOption);
            var json = context.ParseResult.GetValueForOption(EspCommands.JsonOption);
            var verbose = context.ParseResult.GetValueForOption(EspCommands.VerboseOption);

            var logger = EspCommands.CreateLogger(json, verbose);
            var pluginService = new PluginService(logger);
            var propService = new ScriptPropertyService(logger);

            var loadResult = pluginService.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;

            // Find the quest
            var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questId);
            if (quest == null)
            {
                EspCommands.OutputError($"Quest not found: {questId}", json);
                return;
            }

            // Find the script (on quest or on alias)
            ScriptEntry? script = null;
            if (!string.IsNullOrEmpty(aliasTarget))
            {
                // Find script on alias (via QuestFragmentAlias)
                var alias = quest.Aliases.FirstOrDefault(a => a.Name == aliasTarget);
                if (alias == null)
                {
                    EspCommands.OutputError($"Alias not found: {aliasTarget}", json);
                    return;
                }
                script = propService.FindAliasScript(quest, aliasTarget, scriptName);
                if (script == null)
                {
                    EspCommands.OutputError($"Script '{scriptName}' not found on alias '{aliasTarget}'", json,
                        suggestions: new[] { 
                            "Attach the script first with 'esp add-alias --script' or 'esp attach-alias-script'",
                            "Note: Alias scripts are stored in QuestFragmentAlias within the Quest's VirtualMachineAdapter"
                        });
                    return;
                }
            }
            else
            {
                // Find script on quest
                script = propService.FindQuestScript(quest, scriptName);
                if (script == null)
                {
                    EspCommands.OutputError($"Script '{scriptName}' not found on quest '{questId}'", json,
                        suggestions: new[] { "Attach the script first with 'esp attach-script'" });
                    return;
                }
            }

            // Set the property based on type
            bool success = propType.ToLowerInvariant() switch
            {
                "object" => propService.SetObjectProperty(script, propertyName, value),
                "alias" => propService.SetAliasProperty(script, propertyName, quest, value),
                "int" => propService.SetIntProperty(script, propertyName, int.Parse(value)),
                "float" => propService.SetFloatProperty(script, propertyName, float.Parse(value)),
                "bool" => propService.SetBoolProperty(script, propertyName, bool.Parse(value)),
                "string" => propService.SetStringProperty(script, propertyName, value),
                _ => false
            };

            if (!success)
            {
                EspCommands.OutputError($"Failed to set property '{propertyName}'", json);
                return;
            }

            var saveResult = pluginService.SavePlugin(mod, plugin);

            if (json)
            {
                Console.WriteLine(new
                {
                    success = saveResult.Success,
                    result = new { quest = questId, script = scriptName, property = propertyName, type = propType, value },
                    error = saveResult.Error
                }.ToJson());
            }
            else if (saveResult.Success)
            {
                var target = !string.IsNullOrEmpty(aliasTarget) ? $"alias '{aliasTarget}'" : $"quest '{questId}'";
                Console.WriteLine($"Set {propType} property '{propertyName}' = '{value}' on script '{scriptName}' ({target})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static Command CreateAddAliasCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var questOption = new Option<string>("--quest", "Editor ID of the quest to add alias to") { IsRequired = true };
        var nameOption = new Option<string>("--name", "Name for the alias") { IsRequired = true };
        var scriptOption = new Option<string?>("--script", "Script to attach to the alias");
        var flagsOption = new Option<string?>("--flags", 
            getDefaultValue: () => "Optional,AllowReuseInQuest,AllowReserved",
            description: "Comma-separated flags (Optional, AllowReuseInQuest, AllowReserved, etc.)");

        var cmd = new Command("add-alias", "Add a reference alias to a quest")
        {
            pluginArg, questOption, nameOption, scriptOption, flagsOption
        };

        cmd.SetHandler((plugin, questId, aliasName, scriptName, flagsStr, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var pluginService = new PluginService(logger);
            var aliasService = new AliasService(logger);

            var loadResult = pluginService.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;

            // Parse flags
            QuestAlias.Flag flags = 0;
            if (!string.IsNullOrWhiteSpace(flagsStr))
            {
                foreach (var flagName in flagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (Enum.TryParse<QuestAlias.Flag>(flagName, true, out var flag))
                    {
                        flags |= flag;
                    }
                    else
                    {
                        EspCommands.OutputError($"Unknown flag: {flagName}", json);
                        return;
                    }
                }
            }

            var addResult = aliasService.AddReferenceAlias(mod, questId, aliasName, flags);
            if (!addResult.Success)
            {
                EspCommands.OutputError(addResult.Error!, json);
                return;
            }

            var alias = addResult.Value!;

            // If script provided, attach it
            if (!string.IsNullOrEmpty(scriptName))
            {
                var attachResult = aliasService.AttachScriptToAlias(mod, questId, aliasName, scriptName);
                if (!attachResult.Success)
                {
                    EspCommands.OutputError(attachResult.Error!, json);
                    return;
                }
            }

            var saveResult = pluginService.SavePlugin(mod, plugin);

            if (json)
            {
                if (saveResult.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            quest = questId,
                            aliasId = alias.ID,
                            aliasName = aliasName,
                            script = scriptName,
                            flags = flags.ToString()
                        }
                    }.ToJson());
                }
                else
                {
                    Console.WriteLine(new { success = false, error = saveResult.Error }.ToJson());
                    Environment.ExitCode = 1;
                }
            }
            else if (saveResult.Success)
            {
                var scriptInfo = !string.IsNullOrEmpty(scriptName) ? $" with script '{scriptName}'" : "";
                Console.WriteLine($"Added alias [{alias.ID}] '{aliasName}' to quest '{questId}'{scriptInfo} (flags: {flags})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, questOption, nameOption, scriptOption, flagsOption, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateAttachAliasScriptCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var questOption = new Option<string>("--quest", "Editor ID of the quest") { IsRequired = true };
        var aliasOption = new Option<string>("--alias", "Name of the alias") { IsRequired = true };
        var scriptOption = new Option<string>("--script", "Script name to attach") { IsRequired = true };

        var cmd = new Command("attach-alias-script", "Attach a script to a quest alias")
        {
            pluginArg, questOption, aliasOption, scriptOption
        };

        cmd.SetHandler((plugin, questId, aliasName, scriptName, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var pluginService = new PluginService(logger);
            var aliasService = new AliasService(logger);

            var loadResult = pluginService.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;

            var attachResult = aliasService.AttachScriptToAlias(mod, questId, aliasName, scriptName);
            if (!attachResult.Success)
            {
                EspCommands.OutputError(attachResult.Error!, json);
                return;
            }

            var saveResult = pluginService.SavePlugin(mod, plugin);

            if (json)
            {
                Console.WriteLine(new
                {
                    success = saveResult.Success,
                    result = saveResult.Success ? new { quest = questId, alias = aliasName, script = scriptName } : null,
                    error = saveResult.Error
                }.ToJson());
                if (!saveResult.Success) Environment.ExitCode = 1;
            }
            else if (saveResult.Success)
            {
                Console.WriteLine($"Attached script '{scriptName}' to alias '{aliasName}' on quest '{questId}'");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, questOption, aliasOption, scriptOption, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateSetAliasPropertyCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var questOption = new Option<string>("--quest", "Editor ID of the quest") { IsRequired = true };
        var aliasOption = new Option<string>("--alias", "Name of the alias") { IsRequired = true };
        var scriptOption = new Option<string>("--script", "Script name") { IsRequired = true };
        var propNameOption = new Option<string>("--property", "Property name") { IsRequired = true };
        var valueOption = new Option<string>("--value", "Property value") { IsRequired = true };
        var typeOption = new Option<string>("--type", "Property type (object, int, float, bool, string)")
        { IsRequired = true };

        var cmd = new Command("set-alias-property", "Set a property on an alias script")
        {
            pluginArg, questOption, aliasOption, scriptOption, propNameOption, valueOption, typeOption
        };

        cmd.SetHandler((plugin, questId, aliasName, scriptName, propName, value, typeStr, verbose) =>
        {
            var logger = EspCommands.CreateLogger(false, verbose);
            var pluginService = new PluginService(logger);
            var aliasService = new AliasService(logger);
            var propService = new ScriptPropertyService(logger, pluginService);

            var loadResult = pluginService.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                Console.Error.WriteLine($"Error: {loadResult.Error}");
                Environment.ExitCode = 1;
                return;
            }

            var mod = loadResult.Value!;

            // Parse property type
            if (!Enum.TryParse<ScriptProperty.Type>(typeStr, true, out var propType))
            {
                Console.Error.WriteLine($"Invalid property type: {typeStr}. Valid types: object, int, float, bool, string");
                Environment.ExitCode = 1;
                return;
            }

            // Get or create fragment alias
            var fragResult = aliasService.GetOrCreateFragmentAlias(mod, questId, aliasName);
            if (!fragResult.Success)
            {
                Console.Error.WriteLine($"Error: {fragResult.Error}");
                Environment.ExitCode = 1;
                return;
            }

            var fragAlias = fragResult.Value!;

            // Find or add the script
            var script = fragAlias.Scripts.FirstOrDefault(s => s.Name == scriptName);
            if (script == null)
            {
                script = new ScriptEntry { Name = scriptName };
                fragAlias.Scripts.Add(script);
                logger.Info($"Added script '{scriptName}' to alias fragment");
            }

            // Set the property
            var setPropResult = propService.SetPropertyOnScript(mod, script, propName, value, propType, $"alias '{aliasName}'");
            if (!setPropResult.Success)
            {
                Console.Error.WriteLine($"Error: {setPropResult.Error}");
                Environment.ExitCode = 1;
                return;
            }

            var saveResult = pluginService.SavePlugin(mod, plugin);

            if (saveResult.Success)
            {
                Console.WriteLine($"Set {typeStr} property '{propName}' = '{value}' on script '{scriptName}' (alias '{aliasName}', quest '{questId}')");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, questOption, aliasOption, scriptOption, propNameOption, valueOption, typeOption, EspCommands.VerboseOption);

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
            var logger = EspCommands.CreateLogger(json, verbose);
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
        }, pluginArg, outputOption, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }
}
