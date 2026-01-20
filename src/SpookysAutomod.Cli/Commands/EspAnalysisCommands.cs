using System.CommandLine;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Esp.Services;
using Mutagen.Bethesda.Skyrim;

namespace SpookysAutomod.Cli.Commands;

/// <summary>
/// Analysis commands: analyze, analyze-deep, auto-fill, debug-types
/// </summary>
internal static class EspAnalysisCommands
{
    public static void Register(Command parent)
    {
        parent.AddCommand(CreateAnalyzeCommand());
        parent.AddCommand(CreateAnalyzeDeepCommand());
        parent.AddCommand(CreateAutoFillCommand());
        parent.AddCommand(CreateDebugTypesCommand());
    }

    private static Command CreateAnalyzeCommand()
    {
        var pathArg = new Argument<string>("plugin", "Path to the plugin file");

        var cmd = new Command("analyze", "Detailed analysis of plugin records including quest aliases")
        {
            pathArg
        };

        cmd.SetHandler((path, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginReadOnly(path);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;

            Console.WriteLine("=== QUESTS ===");
            foreach (var quest in mod.Quests)
            {
                Console.WriteLine($"\nQuest: {quest.EditorID} ({quest.FormKey})");
                Console.WriteLine($"  Name: {quest.Name}");
                Console.WriteLine($"  Flags: {quest.Flags}");
                Console.WriteLine($"  Priority: {quest.Priority}");

                // Check for scripts
                if (quest.VirtualMachineAdapter is QuestAdapter adapter && adapter.Scripts.Count > 0)
                {
                    Console.WriteLine($"  Scripts:");
                    foreach (var script in adapter.Scripts)
                    {
                        Console.WriteLine($"    - {script.Name}");
                        if (script.Properties.Count > 0)
                        {
                            foreach (var prop in script.Properties)
                            {
                                Console.WriteLine($"        {prop.Name}: {prop.GetType().Name.Replace("Script", "").Replace("Property", "")}");
                            }
                        }
                    }
                }

                // Check aliases
                if (quest.Aliases != null && quest.Aliases.Count > 0)
                {
                    Console.WriteLine($"  Aliases ({quest.Aliases.Count}):");
                    foreach (var alias in quest.Aliases)
                    {
                        Console.WriteLine($"    [{alias.ID}] {alias.Name}");
                        Console.WriteLine($"        Flags: {alias.Flags}");
                        
                        // Check for alias scripts using dynamic
                        dynamic dynAlias = alias;
                        try
                        {
                            var aliasAdapter = dynAlias.VirtualMachineAdapter;
                            if (aliasAdapter != null && aliasAdapter.Scripts.Count > 0)
                            {
                                Console.WriteLine($"        Scripts:");
                                foreach (var script in aliasAdapter.Scripts)
                                {
                                    Console.WriteLine($"          - {script.Name}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"        Scripts: (none)");
                            }
                        }
                        catch 
                        { 
                            Console.WriteLine($"        Scripts: (unable to read)");
                        }
                    }
                }
            }

            Console.WriteLine("\n=== GLOBALS ===");
            foreach (var global in mod.Globals)
            {
                var value = global switch
                {
                    GlobalFloat f => $"{f.Data} (float)",
                    GlobalInt i => $"{i.Data} (int)",
                    GlobalShort s => $"{s.Data} (short)",
                    _ => "?"
                };
                Console.WriteLine($"  {global.EditorID} ({global.FormKey}) = {value}");
            }

            Console.WriteLine("\n=== FACTIONS ===");
            foreach (var faction in mod.Factions)
            {
                Console.WriteLine($"  {faction.EditorID} ({faction.FormKey})");
                Console.WriteLine($"    Flags: {faction.Flags}");
            }

        }, pathArg, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateAnalyzeDeepCommand()
    {
        var pathArg = new Argument<string>("plugin", "Path to the plugin file");
        var questFilterOption = new Option<string?>("--quest", "Filter to specific quest EditorID");
        var sectionsOption = new Option<string?>("--sections", "Comma-separated sections to include (quests,dialogue,keywords,formlists,spells,perks). Default: all");
        var outputOption = new Option<string?>("--output", "Output file path (if not specified, outputs to console)");

        var cmd = new Command("analyze-deep", "Deep analysis with conditions, script properties, stages, and dialogue")
        {
            pathArg,
            questFilterOption,
            sectionsOption,
            outputOption
        };

        cmd.SetHandler((path, questFilter, sections, outputPath, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var loadResult = service.LoadPluginReadOnly(path);
            if (!loadResult.Success)
            {
                EspCommands.OutputError(loadResult.Error!, json);
                return;
            }

            var mod = loadResult.Value!;
            var includeSections = string.IsNullOrEmpty(sections) 
                ? new HashSet<string> { "quests", "dialogue", "keywords", "formlists", "spells", "perks" }
                : new HashSet<string>(sections.Split(',').Select(s => s.Trim().ToLower()));

            // Set up output - file or console
            TextWriter originalOut = Console.Out;
            StreamWriter? fileWriter = null;
            if (!string.IsNullOrEmpty(outputPath))
            {
                fileWriter = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                Console.SetOut(fileWriter);
                originalOut.WriteLine($"Writing analysis to: {outputPath}");
            }

            try
            {
                // === QUESTS (Deep) ===
                if (includeSections.Contains("quests"))
                {
                    OutputQuestsDeep(mod, questFilter);
                }

                // === DIALOGUE ===
                if (includeSections.Contains("dialogue"))
                {
                    OutputDialogue(mod);
                }

                // === KEYWORDS ===
                if (includeSections.Contains("keywords"))
                {
                    Console.WriteLine("\n=== KEYWORDS ===");
                    foreach (var keyword in mod.Keywords)
                    {
                        Console.WriteLine($"  {keyword.EditorID} ({keyword.FormKey})");
                    }
                }

                // === FORM LISTS ===
                if (includeSections.Contains("formlists"))
                {
                    Console.WriteLine("\n=== FORM LISTS ===");
                    foreach (var formList in mod.FormLists)
                    {
                        Console.WriteLine($"\nFormList: {formList.EditorID} ({formList.FormKey})");
                        if (formList.Items != null && formList.Items.Count > 0)
                        {
                            Console.WriteLine($"  Items ({formList.Items.Count}):");
                            foreach (var item in formList.Items)
                            {
                                Console.WriteLine($"    - {item}");
                            }
                        }
                    }
                }

                // === SPELLS ===
                if (includeSections.Contains("spells"))
                {
                    OutputSpells(mod);
                }

                // === PERKS ===
                if (includeSections.Contains("perks"))
                {
                    OutputPerks(mod);
                }

                // === GLOBALS ===
                OutputGlobals(mod);

                // === FACTIONS ===
                OutputFactions(mod);
            }
            finally
            {
                // Restore console and cleanup file writer
                if (fileWriter != null)
                {
                    Console.SetOut(originalOut);
                    fileWriter.Flush();
                    fileWriter.Close();
                    Console.WriteLine($"Analysis complete. Output written to file.");
                }
            }

        }, pathArg, questFilterOption, sectionsOption, outputOption, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static void OutputQuestsDeep(ISkyrimModGetter mod, string? questFilter)
    {
        Console.WriteLine("=== QUESTS (DEEP ANALYSIS) ===");
        foreach (var quest in mod.Quests)
        {
            if (!string.IsNullOrEmpty(questFilter) && 
                !quest.EditorID?.Contains(questFilter, StringComparison.OrdinalIgnoreCase) == true)
                continue;

            Console.WriteLine($"\n{'='} Quest: {quest.EditorID} ({quest.FormKey}) {'='}");
            Console.WriteLine($"  Name: {quest.Name}");
            Console.WriteLine($"  Flags: {quest.Flags}");
            Console.WriteLine($"  Priority: {quest.Priority}");
            Console.WriteLine($"  Type: {quest.Type}");

            // Quest-level scripts with property VALUES
            if (quest.VirtualMachineAdapter is QuestAdapter adapter && adapter.Scripts.Count > 0)
            {
                Console.WriteLine($"  Scripts:");
                foreach (var script in adapter.Scripts)
                {
                    Console.WriteLine($"    - {script.Name}");
                    OutputScriptPropertiesDynamic(script.Properties, "        ");
                }
            }

            // Quest Stages
            if (quest.Stages != null && quest.Stages.Count > 0)
            {
                Console.WriteLine($"  Stages ({quest.Stages.Count}):");
                foreach (var stage in quest.Stages)
                {
                    var stageFlags = stage.Flags != 0 ? $" [{stage.Flags}]" : "";
                    Console.WriteLine($"    Stage {stage.Index}{stageFlags}:");
                    
                    if (stage.LogEntries != null)
                    {
                        foreach (var entry in stage.LogEntries)
                        {
                            if (entry.Entry != null && !string.IsNullOrEmpty(entry.Entry.String))
                            {
                                Console.WriteLine($"      Log: \"{entry.Entry.String}\"");
                            }
                            
                            // Stage conditions
                            if (entry.Conditions != null && entry.Conditions.Count > 0)
                            {
                                Console.WriteLine($"      Conditions ({entry.Conditions.Count}):");
                                OutputConditionsDynamic(entry.Conditions, "        ");
                            }
                        }
                    }
                }
            }

            // Quest Objectives
            if (quest.Objectives != null && quest.Objectives.Count > 0)
            {
                Console.WriteLine($"  Objectives ({quest.Objectives.Count}):");
                foreach (var obj in quest.Objectives)
                {
                    Console.WriteLine($"    [{obj.Index}] {obj.DisplayText}");
                }
            }

            // Aliases with fill conditions and scripts
            if (quest.Aliases != null && quest.Aliases.Count > 0)
            {
                Console.WriteLine($"  Aliases ({quest.Aliases.Count}):");
                foreach (var alias in quest.Aliases)
                {
                    Console.WriteLine($"    [{alias.ID}] {alias.Name}");
                    Console.WriteLine($"        Flags: {alias.Flags}");
                    
                    // Use dynamic to access type-specific properties
                    dynamic dynAlias = alias;
                    try
                    {
                        var conditions = dynAlias.Conditions;
                        if (conditions != null && conditions.Count > 0)
                        {
                            Console.WriteLine($"        Fill Conditions ({conditions.Count}):");
                            OutputConditionsDynamic(conditions, "          ");
                        }
                    }
                    catch { }
                    
                    try
                    {
                        var forcedRef = dynAlias.ForcedReference;
                        if (forcedRef != null && !forcedRef.IsNull)
                        {
                            Console.WriteLine($"        Forced Ref: {forcedRef}");
                        }
                    }
                    catch { }
                    
                    try
                    {
                        var uniqueActor = dynAlias.UniqueActor;
                        if (uniqueActor != null && !uniqueActor.IsNull)
                        {
                            Console.WriteLine($"        Unique Actor: {uniqueActor}");
                        }
                    }
                    catch { }
                    
                    try
                    {
                        var aliasAdapter = dynAlias.VirtualMachineAdapter;
                        if (aliasAdapter != null && aliasAdapter.Scripts != null && aliasAdapter.Scripts.Count > 0)
                        {
                            Console.WriteLine($"        Scripts:");
                            foreach (var script in aliasAdapter.Scripts)
                            {
                                Console.WriteLine($"          - {script.Name}");
                                try
                                {
                                    OutputScriptPropertiesDynamic(script.Properties, "              ");
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }

    private static void OutputDialogue(ISkyrimModGetter mod)
    {
        Console.WriteLine("\n=== DIALOGUE TOPICS ===");
        foreach (var topic in mod.DialogTopics)
        {
            Console.WriteLine($"\nTopic: {topic.EditorID} ({topic.FormKey})");
            Console.WriteLine($"  Name: {topic.Name}");
            Console.WriteLine($"  Flags: {topic.TopicFlags}");
            Console.WriteLine($"  Subtype: {topic.Subtype}");
            
            if (topic.Quest != null && !topic.Quest.IsNull)
            {
                Console.WriteLine($"  Quest: {topic.Quest}");
            }
            
            if (topic.Responses != null && topic.Responses.Count > 0)
            {
                Console.WriteLine($"  Responses ({topic.Responses.Count}):");
                foreach (var response in topic.Responses)
                {
                    Console.WriteLine($"    Response {response.FormKey}:");
                    if (response.Conditions != null && response.Conditions.Count > 0)
                    {
                        Console.WriteLine($"      Conditions:");
                        OutputConditionsDynamic(response.Conditions, "        ");
                    }
                    
                    if (response.Responses != null)
                    {
                        foreach (var line in response.Responses)
                        {
                            try
                            {
                                if (line.Text != null && !string.IsNullOrEmpty(line.Text.String))
                                {
                                    Console.WriteLine($"      Text: \"{line.Text.String}\"");
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }
    }

    private static void OutputSpells(ISkyrimModGetter mod)
    {
        Console.WriteLine("\n=== SPELLS ===");
        foreach (var spell in mod.Spells)
        {
            Console.WriteLine($"\nSpell: {spell.EditorID} ({spell.FormKey})");
            Console.WriteLine($"  Name: {spell.Name}");
            Console.WriteLine($"  Type: {spell.Type}");
            Console.WriteLine($"  Cast Type: {spell.CastType}");
            Console.WriteLine($"  Target Type: {spell.TargetType}");
            Console.WriteLine($"  Base Cost: {spell.BaseCost}");
            
            if (spell.Effects != null && spell.Effects.Count > 0)
            {
                Console.WriteLine($"  Effects ({spell.Effects.Count}):");
                foreach (var effect in spell.Effects)
                {
                    Console.WriteLine($"    - Base Effect: {effect.BaseEffect}");
                    if (effect.Data != null)
                    {
                        Console.WriteLine($"      Magnitude: {effect.Data.Magnitude}");
                        Console.WriteLine($"      Duration: {effect.Data.Duration}");
                        Console.WriteLine($"      Area: {effect.Data.Area}");
                    }
                    
                    if (effect.Conditions != null && effect.Conditions.Count > 0)
                    {
                        Console.WriteLine($"      Conditions:");
                        OutputConditionsDynamic(effect.Conditions, "        ");
                    }
                }
            }
        }
    }

    private static void OutputPerks(ISkyrimModGetter mod)
    {
        Console.WriteLine("\n=== PERKS ===");
        foreach (var perk in mod.Perks)
        {
            Console.WriteLine($"\nPerk: {perk.EditorID} ({perk.FormKey})");
            Console.WriteLine($"  Name: {perk.Name}");
            Console.WriteLine($"  Description: {perk.Description}");
            Console.WriteLine($"  Playable: {perk.Playable}");
            Console.WriteLine($"  Hidden: {perk.Hidden}");
            Console.WriteLine($"  Level: {perk.Level}");
            Console.WriteLine($"  NumRanks: {perk.NumRanks}");
            
            if (perk.NextPerk != null && !perk.NextPerk.IsNull)
            {
                Console.WriteLine($"  NextPerk: {perk.NextPerk}");
            }
            
            if (perk.Conditions != null && perk.Conditions.Count > 0)
            {
                Console.WriteLine($"  Conditions ({perk.Conditions.Count}):");
                OutputConditionsDynamic(perk.Conditions, "    ");
            }
            
            if (perk.Effects != null && perk.Effects.Count > 0)
            {
                Console.WriteLine($"  Effects/Entry Points ({perk.Effects.Count}):");
                foreach (var effect in perk.Effects)
                {
                    OutputPerkEffect(effect);
                }
            }
        }
    }

    private static void OutputPerkEffect(IAPerkEffectGetter effect)
    {
        try
        {
            dynamic dynEffect = effect;
            Console.WriteLine($"    Effect Rank: {dynEffect.Rank}, Priority: {dynEffect.Priority}");
            
            try
            {
                var effectType = effect.GetType().Name;
                Console.WriteLine($"      Type: {effectType}");
                
                if (effectType.Contains("EntryPoint"))
                {
                    try { Console.WriteLine($"      EntryPoint: {dynEffect.EntryPoint}"); } catch { }
                    try { Console.WriteLine($"      Function: {dynEffect.Function}"); } catch { }
                    try { Console.WriteLine($"      Tab Count: {dynEffect.TabCount}"); } catch { }
                    
                    try
                    {
                        var perkConditions = dynEffect.Conditions;
                        if (perkConditions != null && perkConditions.Count > 0)
                        {
                            Console.WriteLine($"      Entry Conditions ({perkConditions.Count}):");
                            OutputConditionsDynamic(perkConditions, "        ");
                        }
                    }
                    catch { }
                    
                    try
                    {
                        var funcData = dynEffect.FunctionParameter;
                        if (funcData != null)
                        {
                            Console.WriteLine($"      FunctionParameter: {funcData}");
                        }
                    }
                    catch { }
                    
                    try
                    {
                        var spell = dynEffect.Spell;
                        if (spell != null && !spell.IsNull)
                        {
                            Console.WriteLine($"      Spell: {spell}");
                        }
                    }
                    catch { }
                    
                    try
                    {
                        var ability = dynEffect.Ability;
                        if (ability != null && !ability.IsNull)
                        {
                            Console.WriteLine($"      Ability: {ability}");
                        }
                    }
                    catch { }
                }
                else if (effectType.Contains("Quest"))
                {
                    try
                    {
                        Console.WriteLine($"      Quest: {dynEffect.Quest}");
                        Console.WriteLine($"      Stage: {dynEffect.Stage}");
                    }
                    catch { }
                }
                else if (effectType.Contains("Ability"))
                {
                    try
                    {
                        Console.WriteLine($"      Ability: {dynEffect.Ability}");
                    }
                    catch { }
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    (Error reading effect: {ex.Message})");
        }
    }

    private static void OutputGlobals(ISkyrimModGetter mod)
    {
        Console.WriteLine("\n=== GLOBALS ===");
        foreach (var global in mod.Globals)
        {
            string value = "?";
            string type = "unknown";
            
            var typeName = global.GetType().Name;
            if (typeName.Contains("Float"))
            {
                type = "float";
                try { value = ((dynamic)global).Data?.ToString() ?? "0"; } catch { }
            }
            else if (typeName.Contains("Int") && !typeName.Contains("Short"))
            {
                type = "int";
                try { value = ((dynamic)global).Data?.ToString() ?? "0"; } catch { }
            }
            else if (typeName.Contains("Short"))
            {
                type = "short";
                try { value = ((dynamic)global).Data?.ToString() ?? "0"; } catch { }
            }
            
            Console.WriteLine($"  {global.EditorID} ({global.FormKey}) = {value} ({type})");
        }
    }

    private static void OutputFactions(ISkyrimModGetter mod)
    {
        Console.WriteLine("\n=== FACTIONS ===");
        foreach (var faction in mod.Factions)
        {
            Console.WriteLine($"  {faction.EditorID} ({faction.FormKey})");
            Console.WriteLine($"    Flags: {faction.Flags}");
            if (faction.Relations != null && faction.Relations.Count > 0)
            {
                Console.WriteLine($"    Relations ({faction.Relations.Count}):");
                foreach (var rel in faction.Relations)
                {
                    Console.WriteLine($"      - {rel.Target}: {rel.Modifier} ({rel.Reaction})");
                }
            }
        }
    }

    /// <summary>
    /// Output script properties with their actual values using dynamic typing
    /// </summary>
    private static void OutputScriptPropertiesDynamic(dynamic properties, string indent)
    {
        if (properties == null) return;
        
        try
        {
            foreach (var prop in properties)
            {
                try
                {
                    string name = prop.Name?.ToString() ?? "?";
                    string typeName = prop.GetType().Name.Replace("Script", "").Replace("Property", "").Replace("Getter", "");
                    string valueStr = "?";
                    
                    try
                    {
                        if (prop.GetType().Name.Contains("Object"))
                        {
                            var obj = prop.Object;
                            if (obj != null && !obj.IsNull)
                                valueStr = obj.FormKey.ToString();
                            else
                                valueStr = "null";
                        }
                        else if (prop.GetType().Name.Contains("Int") || prop.GetType().Name.Contains("Float") || 
                                 prop.GetType().Name.Contains("Bool") || prop.GetType().Name.Contains("String"))
                        {
                            valueStr = prop.Data?.ToString() ?? "null";
                        }
                        else if (prop.GetType().Name.Contains("List"))
                        {
                            try { valueStr = $"[{prop.Data?.Count ?? 0} items]"; } catch { valueStr = "[list]"; }
                        }
                    }
                    catch { valueStr = "(unable to read value)"; }
                    
                    Console.WriteLine($"{indent}{name}: {valueStr} ({typeName})");
                }
                catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Output conditions in a readable format using dynamic typing
    /// </summary>
    private static void OutputConditionsDynamic(dynamic conditions, string indent)
    {
        if (conditions == null) return;
        
        try
        {
            foreach (var condition in conditions)
            {
                try
                {
                    string op = "AND";
                    string compareOp = "==";
                    string funcName = "?";
                    string runOn = "Subject";
                    string compValue = "?";
                    string param1 = "";
                    string param2 = "";
                    
                    try
                    {
                        var flags = condition.Flags;
                        if (flags.HasFlag(Condition.Flag.OR))
                            op = "OR";
                    }
                    catch { }
                    
                    try
                    {
                        compareOp = condition.CompareOperator switch
                        {
                            CompareOperator.EqualTo => "==",
                            CompareOperator.NotEqualTo => "!=",
                            CompareOperator.GreaterThan => ">",
                            CompareOperator.GreaterThanOrEqualTo => ">=",
                            CompareOperator.LessThan => "<",
                            CompareOperator.LessThanOrEqualTo => "<=",
                            _ => "?"
                        };
                    }
                    catch { }
                    
                    try
                    {
                        var data = condition.Data;
                        if (data != null)
                        {
                            runOn = data.RunOnType?.ToString() ?? "Subject";
                            funcName = data.Function?.ToString() ?? "?";
                            
                            try
                            {
                                var p1Rec = data.ParameterOneRecord;
                                if (p1Rec != null && !p1Rec.IsNull)
                                    param1 = p1Rec.FormKey.ToString();
                                else
                                {
                                    try { param1 = data.ParameterOneNumber?.ToString() ?? ""; } catch { }
                                    if (string.IsNullOrEmpty(param1))
                                    {
                                        try { param1 = data.ParameterOneString ?? ""; } catch { }
                                    }
                                }
                            }
                            catch { }
                            
                            try
                            {
                                var p2Rec = data.ParameterTwoRecord;
                                if (p2Rec != null && !p2Rec.IsNull)
                                    param2 = p2Rec.FormKey.ToString();
                                else
                                {
                                    try { param2 = data.ParameterTwoNumber?.ToString() ?? ""; } catch { }
                                    if (string.IsNullOrEmpty(param2))
                                    {
                                        try { param2 = data.ParameterTwoString ?? ""; } catch { }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                    
                    try
                    {
                        compValue = condition.ComparisonValue?.ToString() ?? "?";
                    }
                    catch { }
                    
                    var paramsStr = string.IsNullOrEmpty(param2) ? param1 : $"{param1}, {param2}";
                    if (!string.IsNullOrEmpty(paramsStr))
                        Console.WriteLine($"{indent}[{op}] {funcName}({paramsStr}) {compareOp} {compValue} (RunOn: {runOn})");
                    else
                        Console.WriteLine($"{indent}[{op}] {funcName}() {compareOp} {compValue} (RunOn: {runOn})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{indent}(condition parse error: {ex.Message})");
                }
            }
        }
        catch { }
    }

    private static Command CreateAutoFillCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");
        var questOption = new Option<string>("--quest", "Quest EditorID to auto-fill scripts on") { IsRequired = true };
        var aliasOption = new Option<string?>("--alias", "Optional: specific alias to auto-fill (otherwise all)");
        var scriptOption = new Option<string?>("--script", "Optional: specific script name to auto-fill");
        var propertiesOption = new Option<string?>("--properties", "Comma-separated list of property names to auto-fill (e.g., 'LocTypeInn,LocTypeDungeon')");
        var fromPscOption = new Option<string?>("--from-psc", "Path to .psc file to read property names from (auto-detects if script name matches)");
        var modFolderOption = new Option<string?>("--mod-folder", "Mod folder to search for .psc files (enables automatic PSC detection)");
        var dataFolderOption = new Option<string?>("--data-folder", "Path to Skyrim Data folder (for loading masters)");
        
        var cmd = new Command("auto-fill", "Auto-fill script properties by matching property names to EditorIDs in masters")
        {
            pluginArg,
            questOption,
            aliasOption,
            scriptOption,
            propertiesOption,
            fromPscOption,
            modFolderOption,
            dataFolderOption,
            EspCommands.JsonOption,
            EspCommands.VerboseOption
        };
        
        cmd.SetHandler((context) =>
        {
            var plugin = context.ParseResult.GetValueForArgument(pluginArg);
            var questId = context.ParseResult.GetValueForOption(questOption)!;
            var aliasName = context.ParseResult.GetValueForOption(aliasOption);
            var scriptName = context.ParseResult.GetValueForOption(scriptOption);
            var propertiesStr = context.ParseResult.GetValueForOption(propertiesOption);
            var fromPsc = context.ParseResult.GetValueForOption(fromPscOption);
            var modFolder = context.ParseResult.GetValueForOption(modFolderOption);
            var dataFolder = context.ParseResult.GetValueForOption(dataFolderOption);
            var json = context.ParseResult.GetValueForOption(EspCommands.JsonOption);
            var verbose = context.ParseResult.GetValueForOption(EspCommands.VerboseOption);
            
            var logger = EspCommands.CreateLogger(json, verbose);
            var autoFillService = new AutoFillService(logger);
            
            // Parse properties list or read from PSC
            string[]? propertiesToFill = null;
            if (!string.IsNullOrEmpty(propertiesStr))
            {
                propertiesToFill = propertiesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            else if (!string.IsNullOrEmpty(fromPsc))
            {
                var pscProps = autoFillService.ExtractPropertiesFromPsc(fromPsc);
                if (pscProps.Count > 0)
                {
                    propertiesToFill = pscProps.ToArray();
                    logger.Info($"Read {pscProps.Count} properties from PSC: {string.Join(", ", pscProps)}");
                }
            }
            
            var effectiveModFolder = modFolder ?? Path.GetDirectoryName(Path.GetFullPath(plugin));
            
            var pluginPath = Path.GetFullPath(plugin);
            var pluginDir = Path.GetDirectoryName(pluginPath);
            var effectiveDataFolder = dataFolder ?? FindDataFolder(pluginDir);
            
            if (effectiveDataFolder == null)
            {
                EspCommands.OutputError("Could not determine Data folder. Please specify --data-folder", json);
                return;
            }
            
            try
            {
                var mod = SkyrimMod.CreateFromBinary(pluginPath, SkyrimRelease.SkyrimSE);
                
                var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questId);
                if (quest == null)
                {
                    EspCommands.OutputError($"Quest not found: {questId}", json);
                    return;
                }
                
                var linkCache = autoFillService.CreateManualLinkCache(mod, effectiveDataFolder);
                
                if (linkCache == null)
                {
                    EspCommands.OutputError("Failed to create link cache for auto-fill", json);
                    return;
                }
                
                int totalFilled = 0;
                
                if (quest.VirtualMachineAdapter is QuestAdapter adapter)
                {
                    Dictionary<string, string?>? GetPropertiesForScript(string scriptNameToFind)
                    {
                        if (propertiesToFill != null)
                            return propertiesToFill.ToDictionary(p => p, _ => (string?)null);
                        
                        if (!string.IsNullOrEmpty(effectiveModFolder))
                        {
                            var pscPath = autoFillService.FindPscFile(scriptNameToFind, effectiveModFolder);
                            if (pscPath != null)
                            {
                                var pscProps = autoFillService.ExtractPropertiesWithTypesFromPsc(pscPath);
                                if (pscProps.Count > 0)
                                {
                                    logger.Info($"  Found PSC with {pscProps.Count} properties: {pscPath}");
                                    return pscProps.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
                                }
                            }
                        }
                        
                        return null;
                    }
                    
                    if (string.IsNullOrEmpty(aliasName))
                    {
                        foreach (var script in adapter.Scripts)
                        {
                            if (!string.IsNullOrEmpty(scriptName) && 
                                !string.Equals(script.Name, scriptName, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            logger.Info($"Auto-filling quest script: {script.Name}");
                            var scriptProps = GetPropertiesForScript(script.Name);
                            var filled = scriptProps != null
                                ? autoFillService.AutoFillFromPropertyListWithTypes(script, scriptProps, linkCache)
                                : autoFillService.AutoFillScriptProperties(script, linkCache);
                            totalFilled += filled;
                        }
                    }
                    
                    foreach (var fragAlias in adapter.Aliases)
                    {
                        var aliasIndex = fragAlias.Property?.Alias ?? -1;
                        var currentAliasName = quest.Aliases.FirstOrDefault(a => a.ID == aliasIndex)?.Name;
                        
                        if (!string.IsNullOrEmpty(aliasName) && 
                            !string.Equals(currentAliasName, aliasName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        foreach (var script in fragAlias.Scripts)
                        {
                            if (!string.IsNullOrEmpty(scriptName) && 
                                !string.Equals(script.Name, scriptName, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            logger.Info($"Auto-filling alias script: {script.Name} on {currentAliasName}");
                            var scriptProps = GetPropertiesForScript(script.Name);
                            var filled = scriptProps != null
                                ? autoFillService.AutoFillFromPropertyListWithTypes(script, scriptProps, linkCache)
                                : autoFillService.AutoFillScriptProperties(script, linkCache);
                            totalFilled += filled;
                        }
                    }
                }
                
                mod.WriteToBinary(pluginPath);
                
                if (json)
                {
                    Console.WriteLine(Result.Ok($"Auto-filled {totalFilled} properties").ToJson(true));
                }
                else
                {
                    Console.WriteLine($"Saved plugin: {pluginPath}");
                    Console.WriteLine($"Auto-filled {totalFilled} properties on quest '{questId}'");
                }
            }
            catch (Exception ex)
            {
                EspCommands.OutputError($"Failed to auto-fill: {ex.Message}", json);
            }
        });
        
        return cmd;
    }
    
    private static string? FindDataFolder(string? startPath)
    {
        if (string.IsNullOrEmpty(startPath))
            return null;
            
        var current = startPath;
        while (!string.IsNullOrEmpty(current))
        {
            var skyrimEsm = Path.Combine(current, "Skyrim.esm");
            if (File.Exists(skyrimEsm))
                return current;
            
            var parent = Path.GetDirectoryName(current);
            if (parent == current)
                break;
            current = parent;
        }
        
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Skyrim Special Edition\Data",
            @"D:\Steam\steamapps\common\Skyrim Special Edition\Data",
            @"E:\Steam\steamapps\common\Skyrim Special Edition\Data",
            @"F:\Steam\steamapps\common\Skyrim Special Edition\Data"
        };
        
        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Skyrim.esm")))
                return path;
        }
        
        return null;
    }

    private static Command CreateDebugTypesCommand()
    {
        var pluginArg = new Argument<string?>("plugin", () => null, "Optional: Path to plugin to inspect alias scripts");
        
        var cmd = new Command("debug-types", "Debug: Explore Mutagen alias types (development only)")
        {
            pluginArg
        };
        
        cmd.SetHandler((plugin) =>
        {
            if (!string.IsNullOrEmpty(plugin))
            {
                AliasTypeExplorer.InspectEspAliasScripts(plugin);
            }
            else
            {
                AliasTypeExplorer.ExploreQuestAliasTypes();
            }
        }, pluginArg);
        
        return cmd;
    }
}
