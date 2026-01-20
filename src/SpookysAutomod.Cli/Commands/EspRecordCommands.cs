using System.CommandLine;
using SpookysAutomod.Esp.Builders;
using SpookysAutomod.Esp.Services;
using Mutagen.Bethesda.Skyrim;

namespace SpookysAutomod.Cli.Commands;

/// <summary>
/// Record creation commands: add-quest, add-spell, add-global, add-weapon, add-armor, add-npc, add-book, add-perk, add-faction
/// </summary>
internal static class EspRecordCommands
{
    public static void Register(Command parent)
    {
        parent.AddCommand(CreateAddQuestCommand());
        parent.AddCommand(CreateAddSpellCommand());
        parent.AddCommand(CreateAddGlobalCommand());
        parent.AddCommand(CreateAddWeaponCommand());
        parent.AddCommand(CreateAddArmorCommand());
        parent.AddCommand(CreateAddNpcCommand());
        parent.AddCommand(CreateAddBookCommand());
        parent.AddCommand(CreateAddPerkCommand());
        parent.AddCommand(CreateAddFactionCommand());
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
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
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
           EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateAddSpellCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the spell");
        var nameOption = new Option<string?>("--name", "Display name for the spell");
        var typeOption = new Option<string>("--type", getDefaultValue: () => "spell",
            description: "Spell type: spell, power, lesser-power, ability, cloak");
        var costOption = new Option<uint>("--cost", getDefaultValue: () => 0, description: "Base magicka cost");
        var effectOption = new Option<string?>("--effect",
            description: "Effect preset: damage-health, restore-health, damage-magicka, restore-magicka, damage-stamina, restore-stamina, fortify-health, fortify-magicka, fortify-stamina, fortify-armor, fortify-attack, script");
        var magnitudeOption = new Option<float>("--magnitude", getDefaultValue: () => 25, description: "Effect magnitude (damage/heal amount or buff value)");
        var durationOption = new Option<int>("--duration", getDefaultValue: () => 0, description: "Effect duration in seconds (0 = instant/constant)");
        var scriptOption = new Option<string?>("--script", "Papyrus script name to attach to the effect (for script or cloak effects)");
        var areaOption = new Option<int>("--area", getDefaultValue: () => 0, description: "Area of effect radius (for cloak spells, default 512)");

        var cmd = new Command("add-spell", "Add a spell record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, typeOption, costOption, effectOption, magnitudeOption, durationOption, scriptOption, areaOption
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
            var script = context.ParseResult.GetValueForOption(scriptOption);
            var area = context.ParseResult.GetValueForOption(areaOption);
            var json = context.ParseResult.GetValueForOption(EspCommands.JsonOption);
            var verbose = context.ParseResult.GetValueForOption(EspCommands.VerboseOption);

            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
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
                case "cloak":
                    builder.AsCloakSpell(area > 0 ? area : 512);
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
                    case "script":
                        if (!string.IsNullOrEmpty(script))
                        {
                            if (type.ToLowerInvariant() == "cloak")
                                builder.WithCloakScriptEffect(script, area > 0 ? area : 512, name);
                            else
                                builder.WithScriptedEffect(script, duration, area, name);
                        }
                        else
                        {
                            hasEffect = false;
                            Console.Error.WriteLine("Warning: Script effect requires --script option");
                        }
                        break;
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
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
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
           EspCommands.JsonOption, EspCommands.VerboseOption);

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
            var json = context.ParseResult.GetValueForOption(EspCommands.JsonOption);
            var verbose = context.ParseResult.GetValueForOption(EspCommands.VerboseOption);

            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { EspCommands.OutputError(loadResult.Error!, json); return; }

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
            var json = context.ParseResult.GetValueForOption(EspCommands.JsonOption);
            var verbose = context.ParseResult.GetValueForOption(EspCommands.VerboseOption);

            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { EspCommands.OutputError(loadResult.Error!, json); return; }

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
            var logger = EspCommands.CreateLogger(json, false);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { EspCommands.OutputError(loadResult.Error!, json); return; }

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
        }, pluginArg, editorIdArg, nameOption, levelOption, femaleOption, essentialOption, uniqueOption, EspCommands.JsonOption);

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
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { EspCommands.OutputError(loadResult.Error!, json); return; }

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
        }, pluginArg, editorIdArg, nameOption, textOption, valueOption, weightOption, EspCommands.JsonOption, EspCommands.VerboseOption);

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
            var json = context.ParseResult.GetValueForOption(EspCommands.JsonOption);
            var verbose = context.ParseResult.GetValueForOption(EspCommands.VerboseOption);

            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { EspCommands.OutputError(loadResult.Error!, json); return; }

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

    private static Command CreateAddFactionCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var editorIdArg = new Argument<string>("editorId", "Editor ID for the faction");
        var nameOption = new Option<string?>("--name", "Display name for the faction");
        var hiddenOption = new Option<bool>("--hidden", getDefaultValue: () => true, description: "Hidden from player faction list (default: true)");
        var trackCrimeOption = new Option<bool>("--track-crime", "Enable crime tracking");

        var cmd = new Command("add-faction", "Add a faction record to a plugin")
        {
            pluginArg, editorIdArg, nameOption, hiddenOption, trackCrimeOption
        };

        cmd.SetHandler((plugin, editorId, name, hidden, trackCrime, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginForEdit(plugin);
            if (!loadResult.Success) { EspCommands.OutputError(loadResult.Error!, json); return; }

            var mod = loadResult.Value!;
            var builder = new FactionBuilder(mod, editorId);

            if (!string.IsNullOrEmpty(name)) builder.WithName(name);
            if (hidden) builder.HiddenFromPlayer();
            else builder.VisibleToPlayer();
            if (trackCrime) builder.TrackCrime();

            var faction = builder.Build();
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
                            editorId = faction.EditorID,
                            formId = faction.FormKey.ToString(),
                            name = faction.Name?.String
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
                Console.WriteLine($"Added faction: {faction.EditorID} ({faction.FormKey})");
            }
            else
            {
                Console.Error.WriteLine($"Error: {saveResult.Error}");
                Environment.ExitCode = 1;
            }
        }, pluginArg, editorIdArg, nameOption, hiddenOption, trackCrimeOption, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }
}
