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
        espCommand.AddCommand(CreateAddWeaponCommand());
        espCommand.AddCommand(CreateAddArmorCommand());
        espCommand.AddCommand(CreateAddNpcCommand());
        espCommand.AddCommand(CreateAddBookCommand());
        espCommand.AddCommand(CreateAddPerkCommand());
        espCommand.AddCommand(CreateAttachScriptCommand());
        espCommand.AddCommand(CreateGenerateSeqCommand());
        espCommand.AddCommand(CreateListMastersCommand());
        espCommand.AddCommand(CreateMergeCommand());

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
                Console.WriteLine(result.ToJson(true));
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
                if (saveResult.Success)
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
                else
                {
                    Console.WriteLine(new { success = false, error = saveResult.Error }.ToJson());
                    Environment.ExitCode = 1;
                }
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
        var effectOption = new Option<string?>("--effect",
            description: "Effect preset: damage-health, restore-health, damage-magicka, restore-magicka, damage-stamina, restore-stamina, fortify-health, fortify-magicka, fortify-stamina, fortify-armor, fortify-attack");
        var magnitudeOption = new Option<float>("--magnitude", getDefaultValue: () => 25, description: "Effect magnitude (damage/heal amount or buff value)");
        var durationOption = new Option<int>("--duration", getDefaultValue: () => 0, description: "Effect duration in seconds (0 = instant)");

        var cmd = new Command("add-spell", "Add a spell record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, typeOption, costOption, effectOption, magnitudeOption, durationOption
        };

        cmd.SetHandler((context) =>
        {
            var plugin = context.ParseResult.GetValueForArgument(pluginArg);
            var editorId = context.ParseResult.GetValueForArgument(editorIdArg);
            var name = context.ParseResult.GetValueForOption(nameOption);
            var type = context.ParseResult.GetValueForOption(typeOption) ?? "spell";
            var cost = context.ParseResult.GetValueForOption(costOption);
            var effect = context.ParseResult.GetValueForOption(effectOption);
            var magnitude = context.ParseResult.GetValueForOption(magnitudeOption);
            var duration = context.ParseResult.GetValueForOption(durationOption);
            var json = context.ParseResult.GetValueForOption(_jsonOption);
            var verbose = context.ParseResult.GetValueForOption(_verboseOption);

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

            // Apply effect if specified
            bool hasEffect = false;
            if (!string.IsNullOrEmpty(effect))
            {
                hasEffect = true;
                switch (effect.ToLowerInvariant())
                {
                    case "damage-health": builder.WithDamageHealth(magnitude, duration); break;
                    case "restore-health": builder.WithRestoreHealth(magnitude, duration); break;
                    case "damage-magicka": builder.WithDamageMagicka(magnitude, duration); break;
                    case "restore-magicka": builder.WithRestoreMagicka(magnitude, duration); break;
                    case "damage-stamina": builder.WithDamageStamina(magnitude, duration); break;
                    case "restore-stamina": builder.WithRestoreStamina(magnitude, duration); break;
                    case "fortify-health": builder.WithFortifyHealth(magnitude, duration > 0 ? duration : 60); break;
                    case "fortify-magicka": builder.WithFortifyMagicka(magnitude, duration > 0 ? duration : 60); break;
                    case "fortify-stamina": builder.WithFortifyStamina(magnitude, duration > 0 ? duration : 60); break;
                    case "fortify-armor": builder.WithFortifyArmor(magnitude, duration > 0 ? duration : 60); break;
                    case "fortify-attack": builder.WithFortifyAttackDamage(magnitude / 100f, duration > 0 ? duration : 60); break;
                    default:
                        hasEffect = false;
                        break;
                }
            }

            var spell = builder.Build();
            var saveResult = service.SavePlugin(mod, plugin);

            var effectCount = spell.Effects.Count;
            if (json)
            {
                if (saveResult.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            editorId = spell.EditorID,
                            formId = spell.FormKey.ToString(),
                            name = spell.Name?.String,
                            effectCount
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
                var msg = $"Added spell: {spell.EditorID} ({spell.FormKey})";
                if (hasEffect)
                    msg += $" [{effectCount} effect(s)]";
                else
                    msg += " [No effects - spell will do nothing! Use --effect to add one]";
                Console.WriteLine(msg);
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        });

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
                if (saveResult.Success)
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
                else
                {
                    Console.WriteLine(new { success = false, error = saveResult.Error }.ToJson());
                    Environment.ExitCode = 1;
                }
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

    private static Command CreateAddWeaponCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the weapon");
        var nameOption = new Option<string?>("--name", "Display name for the weapon");
        var typeOption = new Option<string>("--type", getDefaultValue: () => "sword",
            description: "Weapon type: sword, greatsword, dagger, waraxe, battleaxe, mace, warhammer, bow, crossbow, staff");
        var damageOption = new Option<ushort>("--damage", getDefaultValue: () => 10, description: "Base damage");
        var valueOption = new Option<uint>("--value", getDefaultValue: () => 100, description: "Base value");
        var weightOption = new Option<float>("--weight", getDefaultValue: () => 5, description: "Weight");
        var modelOption = new Option<string?>("--model",
            description: "Model path relative to Data/Meshes (e.g., Weapons\\Iron\\IronSword.nif). Use 'iron-sword', 'steel-sword', 'iron-dagger', or 'hunting-bow' for vanilla presets.");

        var cmd = new Command("add-weapon", "Add a weapon record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, typeOption, damageOption, valueOption, weightOption, modelOption
        };

        cmd.SetHandler((context) =>
        {
            var plugin = context.ParseResult.GetValueForArgument(pluginArg);
            var editorId = context.ParseResult.GetValueForArgument(editorIdArg);
            var name = context.ParseResult.GetValueForOption(nameOption);
            var type = context.ParseResult.GetValueForOption(typeOption) ?? "sword";
            var damage = context.ParseResult.GetValueForOption(damageOption);
            var value = context.ParseResult.GetValueForOption(valueOption);
            var weight = context.ParseResult.GetValueForOption(weightOption);
            var model = context.ParseResult.GetValueForOption(modelOption);
            var json = context.ParseResult.GetValueForOption(_jsonOption);
            var verbose = context.ParseResult.GetValueForOption(_verboseOption);

            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { OutputError(loadResult.Error!, json); return; }

            var mod = loadResult.Value!;
            var builder = new WeaponBuilder(mod, editorId)
                .WithDamage(damage)
                .WithValue(value)
                .WithWeight(weight);

            if (!string.IsNullOrEmpty(name)) builder.WithName(name);

            // Apply model if specified
            if (!string.IsNullOrEmpty(model))
            {
                switch (model.ToLowerInvariant())
                {
                    case "iron-sword": builder.WithIronSwordModel(); break;
                    case "steel-sword": builder.WithSteelSwordModel(); break;
                    case "iron-dagger": builder.WithIronDaggerModel(); break;
                    case "hunting-bow": builder.WithHuntingBowModel(); break;
                    default: builder.WithModel(model); break;
                }
            }

            switch (type.ToLowerInvariant())
            {
                case "greatsword": builder.AsGreatsword(); break;
                case "dagger": builder.AsDagger(); break;
                case "waraxe": builder.AsWarAxe(); break;
                case "battleaxe": builder.AsBattleaxe(); break;
                case "mace": builder.AsMace(); break;
                case "warhammer": builder.AsWarhammer(); break;
                case "bow": builder.AsBow(); break;
                case "crossbow": builder.AsCrossbow(); break;
                case "staff": builder.AsStaff(); break;
                default: builder.AsSword(); break;
            }

            var weapon = builder.Build();
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
                            editorId = weapon.EditorID,
                            formId = weapon.FormKey.ToString(),
                            name = weapon.Name?.String,
                            model = weapon.Model?.File.DataRelativePath
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
                Console.WriteLine($"Added weapon: {weapon.EditorID} ({weapon.FormKey})" + (weapon.Model != null ? $" [Model: {weapon.Model.File}]" : " [No model - weapon will be invisible!]"));
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static Command CreateAddArmorCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the armor");
        var nameOption = new Option<string?>("--name", "Display name for the armor");
        var typeOption = new Option<string>("--type", getDefaultValue: () => "light",
            description: "Armor type: light, heavy, clothing");
        var slotOption = new Option<string>("--slot", getDefaultValue: () => "body",
            description: "Body slot: head, body, hands, feet, shield");
        var ratingOption = new Option<float>("--rating", getDefaultValue: () => 10, description: "Armor rating");
        var valueOption = new Option<uint>("--value", getDefaultValue: () => 100, description: "Base value");
        var modelOption = new Option<string?>("--model",
            description: "Model path relative to Data/Meshes. Presets: 'iron-cuirass', 'iron-helmet', 'iron-gauntlets', 'iron-boots', 'iron-shield'");

        var cmd = new Command("add-armor", "Add an armor record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, typeOption, slotOption, ratingOption, valueOption, modelOption
        };

        cmd.SetHandler((context) =>
        {
            var plugin = context.ParseResult.GetValueForArgument(pluginArg);
            var editorId = context.ParseResult.GetValueForArgument(editorIdArg);
            var name = context.ParseResult.GetValueForOption(nameOption);
            var type = context.ParseResult.GetValueForOption(typeOption) ?? "light";
            var slot = context.ParseResult.GetValueForOption(slotOption) ?? "body";
            var rating = context.ParseResult.GetValueForOption(ratingOption);
            var value = context.ParseResult.GetValueForOption(valueOption);
            var model = context.ParseResult.GetValueForOption(modelOption);
            var json = context.ParseResult.GetValueForOption(_jsonOption);
            var verbose = context.ParseResult.GetValueForOption(_verboseOption);

            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { OutputError(loadResult.Error!, json); return; }

            var mod = loadResult.Value!;
            var builder = new ArmorBuilder(mod, editorId)
                .WithArmorRating(rating)
                .WithValue(value);

            if (!string.IsNullOrEmpty(name)) builder.WithName(name);

            // Apply model if specified
            if (!string.IsNullOrEmpty(model))
            {
                switch (model.ToLowerInvariant())
                {
                    case "iron-cuirass": builder.WithIronCuirassModel(); break;
                    case "iron-helmet": builder.WithIronHelmetModel(); break;
                    case "iron-gauntlets": builder.WithIronGauntletsModel(); break;
                    case "iron-boots": builder.WithIronBootsModel(); break;
                    case "iron-shield": builder.WithIronShieldModel(); break;
                    default: builder.WithWorldModel(model); break;
                }
            }

            switch (type.ToLowerInvariant())
            {
                case "heavy": builder.AsHeavyArmor(); break;
                case "clothing": builder.AsClothing(); break;
                default: builder.AsLightArmor(); break;
            }

            switch (slot.ToLowerInvariant())
            {
                case "head": builder.ForHead(); break;
                case "hands": builder.ForHands(); break;
                case "feet": builder.ForFeet(); break;
                case "shield": builder.ForShield(); break;
                default: builder.ForBody(); break;
            }

            var armor = builder.Build();
            var saveResult = service.SavePlugin(mod, plugin);

            var hasModel = armor.WorldModel?.Male?.Model?.File != null;
            if (json)
            {
                if (saveResult.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            editorId = armor.EditorID,
                            formId = armor.FormKey.ToString(),
                            name = armor.Name?.String,
                            model = armor.WorldModel?.Male?.Model?.File?.DataRelativePath
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
                Console.WriteLine($"Added armor: {armor.EditorID} ({armor.FormKey})" + (hasModel ? $" [Model: {armor.WorldModel?.Male?.Model?.File}]" : " [No model - armor will be invisible!]"));
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static Command CreateAddNpcCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the NPC");
        var nameOption = new Option<string?>("--name", "Display name for the NPC");
        var levelOption = new Option<short>("--level", getDefaultValue: () => 1, description: "NPC level");
        var femaleOption = new Option<bool>("--female", "NPC is female");
        var essentialOption = new Option<bool>("--essential", "NPC is essential");
        var uniqueOption = new Option<bool>("--unique", "NPC is unique");

        var cmd = new Command("add-npc", "Add an NPC record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, levelOption, femaleOption, essentialOption, uniqueOption
        };

        cmd.SetHandler((plugin, editorId, name, level, female, essential, unique, json) =>
        {
            var logger = CreateLogger(json, false);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { OutputError(loadResult.Error!, json); return; }

            var mod = loadResult.Value!;
            var builder = new NpcBuilder(mod, editorId).WithLevel(level);

            if (!string.IsNullOrEmpty(name)) builder.WithName(name);
            if (female) builder.AsFemale();
            if (essential) builder.AsEssential();
            if (unique) builder.AsUnique();

            var npc = builder.Build();
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
                            editorId = npc.EditorID,
                            formId = npc.FormKey.ToString(),
                            name = npc.Name?.String
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
                Console.WriteLine($"Added NPC: {npc.EditorID} ({npc.FormKey})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, editorIdArg, nameOption, levelOption, femaleOption, essentialOption, uniqueOption, _jsonOption);

        return cmd;
    }

    private static Command CreateAddBookCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the book");
        var nameOption = new Option<string?>("--name", "Display name for the book");
        var textOption = new Option<string?>("--text", "Book text content");
        var valueOption = new Option<uint>("--value", getDefaultValue: () => 10, description: "Base value");
        var weightOption = new Option<float>("--weight", getDefaultValue: () => 1, description: "Weight");

        var cmd = new Command("add-book", "Add a book record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, textOption, valueOption, weightOption
        };

        cmd.SetHandler((plugin, editorId, name, text, value, weight, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { OutputError(loadResult.Error!, json); return; }

            var mod = loadResult.Value!;
            var builder = new BookBuilder(mod, editorId)
                .WithValue(value)
                .WithWeight(weight);

            if (!string.IsNullOrEmpty(name)) builder.WithName(name);
            if (!string.IsNullOrEmpty(text)) builder.WithText(text);

            var book = builder.Build();
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
                            editorId = book.EditorID,
                            formId = book.FormKey.ToString(),
                            name = book.Name?.String
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
                Console.WriteLine($"Added book: {book.EditorID} ({book.FormKey})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, editorIdArg, nameOption, textOption, valueOption, weightOption, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateAddPerkCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the perk");
        var nameOption = new Option<string?>("--name", "Display name for the perk");
        var descOption = new Option<string?>("--description", "Perk description");
        var playableOption = new Option<bool>("--playable", "Perk is playable");
        var hiddenOption = new Option<bool>("--hidden", "Perk is hidden");
        var effectOption = new Option<string?>("--effect",
            description: "Effect preset: weapon-damage, damage-reduction, armor, spell-cost, spell-power, spell-duration, sneak-attack, pickpocket, prices");
        var bonusOption = new Option<float>("--bonus", getDefaultValue: () => 25, description: "Bonus percentage (e.g., 25 for +25%)");

        var cmd = new Command("add-perk", "Add a perk record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, descOption, playableOption, hiddenOption, effectOption, bonusOption
        };

        cmd.SetHandler((context) =>
        {
            var plugin = context.ParseResult.GetValueForArgument(pluginArg);
            var editorId = context.ParseResult.GetValueForArgument(editorIdArg);
            var name = context.ParseResult.GetValueForOption(nameOption);
            var desc = context.ParseResult.GetValueForOption(descOption);
            var playable = context.ParseResult.GetValueForOption(playableOption);
            var hidden = context.ParseResult.GetValueForOption(hiddenOption);
            var effect = context.ParseResult.GetValueForOption(effectOption);
            var bonus = context.ParseResult.GetValueForOption(bonusOption);
            var json = context.ParseResult.GetValueForOption(_jsonOption);
            var verbose = context.ParseResult.GetValueForOption(_verboseOption);

            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { OutputError(loadResult.Error!, json); return; }

            var mod = loadResult.Value!;
            var builder = new PerkBuilder(mod, editorId);

            if (!string.IsNullOrEmpty(name)) builder.WithName(name);
            if (!string.IsNullOrEmpty(desc)) builder.WithDescription(desc);
            if (playable) builder.AsPlayable();
            if (hidden) builder.AsHidden();

            // Apply effect if specified
            bool hasEffect = false;
            if (!string.IsNullOrEmpty(effect))
            {
                hasEffect = true;
                switch (effect.ToLowerInvariant())
                {
                    case "weapon-damage": builder.WithWeaponDamageBonus(bonus); break;
                    case "damage-reduction": builder.WithDamageReduction(bonus); break;
                    case "armor": builder.WithArmorBonus(bonus); break;
                    case "spell-cost": builder.WithSpellCostReduction(bonus); break;
                    case "spell-power": builder.WithSpellPowerBonus(bonus); break;
                    case "spell-duration": builder.WithSpellDurationBonus(bonus); break;
                    case "sneak-attack": builder.WithSneakAttackBonus(1.0f + bonus / 100f); break;
                    case "pickpocket": builder.WithPickpocketBonus(bonus); break;
                    case "prices": builder.WithBetterPrices(bonus); break;
                    default:
                        hasEffect = false;
                        break;
                }
            }

            var perk = builder.Build();
            var saveResult = service.SavePlugin(mod, plugin);

            var effectCount = perk.Effects.Count;
            if (json)
            {
                if (saveResult.Success)
                {
                    Console.WriteLine(new
                    {
                        success = true,
                        result = new
                        {
                            editorId = perk.EditorID,
                            formId = perk.FormKey.ToString(),
                            name = perk.Name?.String,
                            effectCount
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
                var msg = $"Added perk: {perk.EditorID} ({perk.FormKey})";
                if (hasEffect)
                    msg += $" [{effectCount} entry/entries]";
                else
                    msg += " [No entries - perk will do nothing! Use --effect to add one]";
                Console.WriteLine(msg);
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        });

        return cmd;
    }

    private static Command CreateListMastersCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");

        var cmd = new Command("list-masters", "List master file dependencies")
        {
            pluginArg
        };

        cmd.SetHandler((plugin, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.GetPluginInfo(plugin);

            if (json)
            {
                Console.WriteLine(new { success = result.Success, result = new { masters = result.Value?.MasterFiles }, error = result.Error }.ToJson());
            }
            else if (result.Success && result.Value != null)
            {
                Console.WriteLine($"Master files for {result.Value.FileName}:");
                if (result.Value.MasterFiles.Count == 0)
                    Console.WriteLine("  (none)");
                else
                    foreach (var master in result.Value.MasterFiles)
                        Console.WriteLine($"  - {master}");
            }
            else
            { Console.Error.WriteLine($"Error: {result.Error}"); Environment.ExitCode = 1; }
        }, pluginArg, _jsonOption, _verboseOption);

        return cmd;
    }

    private static Command CreateMergeCommand()
    {
        var sourceArg = new Argument<string>("source", "Source plugin to merge from");
        var targetArg = new Argument<string>("target", "Target plugin to merge into");
        var outputOption = new Option<string?>("--output", "Output path (defaults to overwriting target)");

        var cmd = new Command("merge", "Merge records from one plugin into another")
        {
            sourceArg, targetArg, outputOption
        };

        cmd.SetHandler((source, target, output, json, verbose) =>
        {
            var logger = CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var sourceResult = service.LoadPluginReadOnly(source);
            if (!sourceResult.Success) { OutputError(sourceResult.Error!, json); return; }

            var targetResult = service.LoadPluginForEdit(target);
            if (!targetResult.Success) { OutputError(targetResult.Error!, json); return; }

            var sourceMod = sourceResult.Value!;
            var targetMod = targetResult.Value!;

            int recordsCopied = 0;

            // Copy all major record types using DeepCopy
            foreach (var quest in sourceMod.Quests) { targetMod.Quests.Add(quest.DeepCopy()); recordsCopied++; }
            foreach (var spell in sourceMod.Spells) { targetMod.Spells.Add(spell.DeepCopy()); recordsCopied++; }
            foreach (var global in sourceMod.Globals) { targetMod.Globals.Add(global.DeepCopy()); recordsCopied++; }
            foreach (var weapon in sourceMod.Weapons) { targetMod.Weapons.Add(weapon.DeepCopy()); recordsCopied++; }
            foreach (var armor in sourceMod.Armors) { targetMod.Armors.Add(armor.DeepCopy()); recordsCopied++; }
            foreach (var npc in sourceMod.Npcs) { targetMod.Npcs.Add(npc.DeepCopy()); recordsCopied++; }
            foreach (var book in sourceMod.Books) { targetMod.Books.Add(book.DeepCopy()); recordsCopied++; }
            foreach (var perk in sourceMod.Perks) { targetMod.Perks.Add(perk.DeepCopy()); recordsCopied++; }

            var saveResult = service.SavePlugin(targetMod, output ?? target);

            if (json)
                Console.WriteLine(new { success = saveResult.Success, result = new { recordsCopied, outputPath = output ?? target }, error = saveResult.Error }.ToJson());
            else if (saveResult.Success)
                Console.WriteLine($"Merged {recordsCopied} records from {source} into {output ?? target}");
            else
            { Console.Error.WriteLine($"Error: {saveResult.Error}"); Environment.ExitCode = 1; }
        }, sourceArg, targetArg, outputOption, _jsonOption, _verboseOption);

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
