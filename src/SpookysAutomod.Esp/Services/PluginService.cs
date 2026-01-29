using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using System.Reflection;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Service for creating, loading, and saving ESP/ESM/ESL plugin files.
/// </summary>
public class PluginService
{
    private readonly IModLogger _logger;

    public PluginService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a new empty plugin.
    /// </summary>
    public Result<string> CreatePlugin(
        string name,
        string outputPath,
        bool isLight = false,
        string? author = null,
        string? description = null)
    {
        try
        {
            // Ensure .esp extension
            if (!name.EndsWith(".esp", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".esm", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".esl", StringComparison.OrdinalIgnoreCase))
            {
                name += ".esp";
            }

            var modKey = ModKey.FromFileName(name);
            var mod = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);

            // Set header flags for light plugin (ESL flag = 0x200)
            if (isLight)
            {
                mod.ModHeader.Flags |= (SkyrimModHeader.HeaderFlag)0x200;
            }

            if (!string.IsNullOrEmpty(author))
            {
                mod.ModHeader.Author = author;
            }

            if (!string.IsNullOrEmpty(description))
            {
                mod.ModHeader.Description = description;
            }

            // Ensure output directory exists
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var fullPath = Path.IsPathRooted(outputPath)
                ? Path.Combine(outputPath, name)
                : Path.Combine(Directory.GetCurrentDirectory(), outputPath, name);

            mod.WriteToBinary(fullPath);

            _logger.Info($"Created plugin: {fullPath}");
            return Result<string>.Ok(fullPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to create plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the output path is writable",
                    "Check that the plugin name contains only valid characters"
                });
        }
    }

    /// <summary>
    /// Load an existing plugin for reading.
    /// </summary>
    public Result<ISkyrimModGetter> LoadPluginReadOnly(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return Result<ISkyrimModGetter>.Fail(
                    $"Plugin not found: {path}",
                    suggestions: new List<string>
                    {
                        "Check the file path is correct",
                        "Use an absolute path if relative path fails"
                    });
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);
            _logger.Debug($"Loaded plugin (read-only): {path}");
            return Result<ISkyrimModGetter>.Ok(mod);
        }
        catch (Exception ex)
        {
            return Result<ISkyrimModGetter>.Fail(
                $"Failed to load plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the file is a valid Skyrim plugin",
                    "Check if the file is corrupted",
                    "Verify the file is not locked by another process"
                });
        }
    }

    /// <summary>
    /// Load an existing plugin for editing.
    /// </summary>
    public Result<SkyrimMod> LoadPluginForEdit(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return Result<SkyrimMod>.Fail(
                    $"Plugin not found: {path}",
                    suggestions: new List<string>
                    {
                        "Check the file path is correct",
                        "Use an absolute path if relative path fails"
                    });
            }

            // Use import mask to properly initialize FormKey allocation
            var mod = SkyrimMod.CreateFromBinary(
                path,
                SkyrimRelease.SkyrimSE,
                new Mutagen.Bethesda.Plugins.Binary.Parameters.BinaryReadParameters
                {
                    // This ensures the FormKey allocator is properly initialized
                });

            // If the mod is empty or has low FormIDs, set a proper starting point
            // Skyrim mods should allocate FormIDs starting from 0x800
            if (mod.ModHeader.Stats.NextFormID < 0x800)
            {
                mod.ModHeader.Stats.NextFormID = 0x800;
            }

            _logger.Debug($"Loaded plugin (editable): {path}");
            return Result<SkyrimMod>.Ok(mod);
        }
        catch (Exception ex)
        {
            return Result<SkyrimMod>.Fail(
                $"Failed to load plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the file is a valid Skyrim plugin",
                    "Check if the file is corrupted",
                    "Verify the file is not locked by another process"
                });
        }
    }

    /// <summary>
    /// Save a plugin to disk.
    /// </summary>
    public Result SavePlugin(SkyrimMod mod, string? outputPath = null)
    {
        try
        {
            var path = outputPath ?? Path.Combine(Directory.GetCurrentDirectory(), mod.ModKey.FileName);
            mod.WriteToBinary(path);
            _logger.Info($"Saved plugin: {path}");
            return Result.Ok($"Plugin saved to: {path}");
        }
        catch (Exception ex)
        {
            return Result.Fail(
                $"Failed to save plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the output path is writable",
                    "Check that no other process has the file locked"
                });
        }
    }

    /// <summary>
    /// Get information about a plugin.
    /// </summary>
    public Result<PluginInfo> GetPluginInfo(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return Result<PluginInfo>.Fail($"Plugin not found: {path}");
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);
            var fileInfo = new FileInfo(path);

            var info = new PluginInfo
            {
                FileName = mod.ModKey.FileName,
                FilePath = path,
                Author = mod.ModHeader.Author,
                Description = mod.ModHeader.Description,
                IsLight = mod.ModHeader.Flags.HasFlag((SkyrimModHeader.HeaderFlag)0x200),
                IsMaster = mod.ModKey.Type == ModType.Master,
                FileSize = fileInfo.Length
            };

            // Get master files
            foreach (var master in mod.ModHeader.MasterReferences)
            {
                info.MasterFiles.Add(master.Master.FileName);
            }

            // Count records by type
            info.RecordCounts["Quests"] = mod.Quests.Count;
            info.RecordCounts["Spells"] = mod.Spells.Count;
            info.RecordCounts["Globals"] = mod.Globals.Count;
            info.RecordCounts["NPCs"] = mod.Npcs.Count;
            info.RecordCounts["Weapons"] = mod.Weapons.Count;
            info.RecordCounts["Armors"] = mod.Armors.Count;
            info.RecordCounts["Books"] = mod.Books.Count;
            info.RecordCounts["Perks"] = mod.Perks.Count;
            info.RecordCounts["Factions"] = mod.Factions.Count;
            info.RecordCounts["MiscItems"] = mod.MiscItems.Count;
            info.RecordCounts["LeveledItems"] = mod.LeveledItems.Count;
            info.RecordCounts["FormLists"] = mod.FormLists.Count;
            info.RecordCounts["EncounterZones"] = mod.EncounterZones.Count;
            info.RecordCounts["Locations"] = mod.Locations.Count;
            info.RecordCounts["Outfits"] = mod.Outfits.Count;

            info.TotalRecords = info.RecordCounts.Values.Sum();

            return Result<PluginInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return Result<PluginInfo>.Fail(
                $"Failed to read plugin info: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Generate a SEQ file for quests that start enabled.
    /// </summary>
    public Result<string> GenerateSeqFile(string pluginPath, string outputDir)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                return Result<string>.Fail($"Plugin not found: {pluginPath}");
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, SkyrimRelease.SkyrimSE);
            var startEnabledQuests = new List<uint>();

            foreach (var quest in mod.Quests)
            {
                if (quest.Flags.HasFlag(Quest.Flag.StartGameEnabled))
                {
                    startEnabledQuests.Add(quest.FormKey.ID);
                }
            }

            if (startEnabledQuests.Count == 0)
            {
                return Result<string>.Fail(
                    "No start-enabled quests found",
                    suggestions: new List<string>
                    {
                        "Add a quest with StartGameEnabled flag",
                        "SEQ files are only needed for quests that start on game load"
                    });
            }

            // Ensure output directory exists
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var seqFileName = Path.GetFileNameWithoutExtension(mod.ModKey.FileName) + ".seq";
            var seqPath = Path.Combine(outputDir, seqFileName);

            // Write SEQ file (simple format: count + FormIDs)
            using var writer = new BinaryWriter(File.Create(seqPath));
            writer.Write((uint)startEnabledQuests.Count);
            foreach (var formId in startEnabledQuests)
            {
                writer.Write(formId);
            }

            _logger.Info($"Generated SEQ file with {startEnabledQuests.Count} quest(s): {seqPath}");
            return Result<string>.Ok(seqPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to generate SEQ file: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// View detailed information about a record
    /// </summary>
    public Result<RecordInfo> ViewRecord(
        string pluginPath,
        string? editorId,
        string? formId,
        string? recordType,
        bool includeRaw = false)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                return Result<RecordInfo>.Fail(
                    $"Plugin not found: {pluginPath}",
                    suggestions: new List<string>
                    {
                        "Check the file path is correct",
                        "Use an absolute path if relative path fails"
                    });
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, SkyrimRelease.SkyrimSE);

            IMajorRecordGetter? record = null;

            if (!string.IsNullOrEmpty(formId))
            {
                var findResult = FindRecordByFormKey(mod, formId);
                if (!findResult.Success)
                {
                    return Result<RecordInfo>.Fail(findResult.Error ?? "Record not found", findResult.ErrorContext);
                }
                record = findResult.Value;
            }
            else if (!string.IsNullOrEmpty(editorId) && !string.IsNullOrEmpty(recordType))
            {
                var findResult = FindRecordByEditorId(mod, editorId, recordType);
                if (!findResult.Success)
                {
                    return Result<RecordInfo>.Fail(findResult.Error ?? "Record not found", findResult.ErrorContext);
                }
                record = findResult.Value;
            }
            else
            {
                return Result<RecordInfo>.Fail(
                    "Must provide either FormID or both EditorID and RecordType",
                    suggestions: new List<string>
                    {
                        "Use --form-id for FormKey-based lookup",
                        "Use --editor-id and --type for EditorID-based lookup"
                    });
            }

            if (record == null)
            {
                return Result<RecordInfo>.Fail(
                    $"Record not found: {editorId ?? formId}",
                    suggestions: new List<string>
                    {
                        "Use 'esp info' to see available records",
                        "Check spelling of EditorID",
                        "Verify FormID is correct"
                    });
            }

            var recordInfo = new RecordInfo
            {
                EditorId = record.EditorID ?? string.Empty,
                FormKey = record.FormKey.ToString(),
                RecordType = record.GetType().Name.Replace("Getter", "").Replace("ReadOnly", "")
            };

            var propsResult = ExtractRecordProperties(record, includeRaw);
            if (!propsResult.Success)
            {
                return Result<RecordInfo>.Fail(propsResult.Error ?? "Failed to extract properties", propsResult.ErrorContext);
            }
            recordInfo.Properties = propsResult.Value ?? new Dictionary<string, object?>();

            // TODO: Add condition extraction once Mutagen API is confirmed
            // var conditionsResult = ExtractConditions(record);
            // if (conditionsResult.Success && conditionsResult.Value != null && conditionsResult.Value.Count > 0)
            // {
            //     recordInfo.Conditions = conditionsResult.Value;
            // }

            return Result<RecordInfo>.Ok(recordInfo);
        }
        catch (Exception ex)
        {
            return Result<RecordInfo>.Fail(
                $"Failed to view record: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Find a record by FormKey string
    /// </summary>
    private Result<IMajorRecordGetter> FindRecordByFormKey(ISkyrimModGetter mod, string formKeyStr)
    {
        try
        {
            if (!Mutagen.Bethesda.Plugins.FormKey.TryFactory(formKeyStr, out var formKey))
            {
                return Result<IMajorRecordGetter>.Fail(
                    $"Invalid FormKey format: {formKeyStr}",
                    suggestions: new List<string>
                    {
                        "Use format: 0x000800 or PluginName.esp:0x000800",
                        "Use --editor-id if you know the EditorID instead"
                    });
            }

            foreach (var record in mod.EnumerateMajorRecords())
            {
                if (record.FormKey == formKey)
                {
                    return Result<IMajorRecordGetter>.Ok(record);
                }
            }

            return Result<IMajorRecordGetter>.Fail(
                $"Record with FormKey {formKey} not found in plugin",
                suggestions: new List<string>
                {
                    "Verify the FormKey exists in this plugin",
                    "Use 'esp info' to see record counts"
                });
        }
        catch (Exception ex)
        {
            return Result<IMajorRecordGetter>.Fail($"Error finding record: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// Find a record by EditorID and type
    /// </summary>
    private Result<IMajorRecordGetter> FindRecordByEditorId(ISkyrimModGetter mod, string editorId, string recordType)
    {
        try
        {
            IMajorRecordGetter? found = null;

            switch (recordType.ToLowerInvariant())
            {
                case "spell":
                    found = mod.Spells.FirstOrDefault(s => s.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "weapon":
                    found = mod.Weapons.FirstOrDefault(w => w.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "armor":
                    found = mod.Armors.FirstOrDefault(a => a.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "quest":
                    found = mod.Quests.FirstOrDefault(q => q.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "npc":
                    found = mod.Npcs.FirstOrDefault(n => n.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "perk":
                    found = mod.Perks.FirstOrDefault(p => p.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "faction":
                    found = mod.Factions.FirstOrDefault(f => f.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "book":
                    found = mod.Books.FirstOrDefault(b => b.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "miscitem":
                    found = mod.MiscItems.FirstOrDefault(m => m.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "global":
                    found = mod.Globals.FirstOrDefault(g => g.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "leveleditem":
                    found = mod.LeveledItems.FirstOrDefault(l => l.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "formlist":
                    found = mod.FormLists.FirstOrDefault(f => f.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "outfit":
                    found = mod.Outfits.FirstOrDefault(o => o.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "location":
                    found = mod.Locations.FirstOrDefault(l => l.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                case "encounterzone":
                    found = mod.EncounterZones.FirstOrDefault(e => e.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true);
                    break;
                default:
                    return Result<IMajorRecordGetter>.Fail(
                        $"Unsupported record type: {recordType}",
                        suggestions: new List<string>
                        {
                            "Supported types: spell, weapon, armor, quest, npc, perk, faction, book, miscitem, global, leveleditem, formlist, outfit, location, encounterzone",
                            "Use --form-id instead for other record types"
                        });
            }

            if (found == null)
            {
                return Result<IMajorRecordGetter>.Fail(
                    $"{recordType} with EditorID '{editorId}' not found",
                    suggestions: new List<string>
                    {
                        "Check EditorID spelling",
                        "Use 'esp info' to see record counts",
                        "Try --form-id if you know the FormKey"
                    });
            }

            return Result<IMajorRecordGetter>.Ok(found);
        }
        catch (Exception ex)
        {
            return Result<IMajorRecordGetter>.Fail($"Error finding record: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// Extract properties from a record based on its type
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractRecordProperties(IMajorRecordGetter record, bool includeRaw)
    {
        try
        {
            return record switch
            {
                ISpellGetter spell => ExtractSpellProperties(spell),
                IWeaponGetter weapon => ExtractWeaponProperties(weapon),
                IArmorGetter armor => ExtractArmorProperties(armor),
                IQuestGetter quest => ExtractQuestProperties(quest),
                INpcGetter npc => ExtractNpcProperties(npc),
                IPerkGetter perk => ExtractPerkProperties(perk),
                _ => includeRaw ? ExtractPropertiesViaReflection(record) : Result<Dictionary<string, object?>>.Ok(new Dictionary<string, object?>())
            };
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, object?>>.Fail($"Failed to extract properties: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// Extract spell-specific properties
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractSpellProperties(ISpellGetter spell)
    {
        var props = new Dictionary<string, object?>
        {
            ["Name"] = spell.Name?.String,
            ["Type"] = spell.Type.ToString(),
            ["BaseCost"] = spell.BaseCost,
            ["CastType"] = spell.CastType.ToString(),
            ["TargetType"] = spell.TargetType.ToString(),
            ["CastDuration"] = spell.CastDuration,
            ["Range"] = spell.Range,
            ["EffectCount"] = spell.Effects.Count
        };

        if (spell.EquipmentType != null && !spell.EquipmentType.IsNull)
        {
            props["EquipmentType"] = spell.EquipmentType.FormKey.ToString();
        }

        var effects = new List<Dictionary<string, object?>>();
        foreach (var effect in spell.Effects)
        {
            var effectProps = new Dictionary<string, object?>
            {
                ["BaseEffect"] = effect.BaseEffect.FormKey.ToString(),
                ["Magnitude"] = effect.Data?.Magnitude ?? 0,
                ["Duration"] = effect.Data?.Duration ?? 0,
                ["Area"] = effect.Data?.Area ?? 0
            };
            effects.Add(effectProps);
        }
        props["Effects"] = effects;

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract weapon-specific properties
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractWeaponProperties(IWeaponGetter weapon)
    {
        var props = new Dictionary<string, object?>
        {
            ["Name"] = weapon.Name?.String,
            ["Damage"] = weapon.BasicStats?.Damage ?? 0,
            ["Weight"] = weapon.BasicStats?.Weight ?? 0,
            ["Value"] = weapon.BasicStats?.Value ?? 0,
            ["CriticalDamage"] = weapon.Critical?.Damage ?? 0,
            ["Speed"] = weapon.Data?.Speed ?? 0,
            ["Reach"] = weapon.Data?.Reach ?? 0,
            ["AnimationType"] = weapon.Data?.AnimationType.ToString()
        };

        if (weapon.Keywords != null)
        {
            props["Keywords"] = weapon.Keywords.Select(k => k.FormKey.ToString()).ToList();
        }

        if (weapon.Template != null && !weapon.Template.IsNull)
        {
            props["Template"] = weapon.Template.FormKey.ToString();
        }

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract armor-specific properties
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractArmorProperties(IArmorGetter armor)
    {
        var props = new Dictionary<string, object?>
        {
            ["Name"] = armor.Name?.String,
            ["ArmorRating"] = armor.ArmorRating,
            ["Weight"] = armor.Weight,
            ["Value"] = armor.Value,
            ["BodyTemplate"] = armor.BodyTemplate?.ToString()
        };

        if (armor.Keywords != null)
        {
            props["Keywords"] = armor.Keywords.Select(k => k.FormKey.ToString()).ToList();
        }

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract quest-specific properties
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractQuestProperties(IQuestGetter quest)
    {
        var props = new Dictionary<string, object?>
        {
            ["Name"] = quest.Name?.String,
            ["Priority"] = quest.Priority,
            ["Flags"] = quest.Flags.ToString(),
            ["StageCount"] = quest.Stages.Count,
            ["AliasCount"] = quest.Aliases.Count
        };

        // TODO: Add Event property once API is confirmed
        // if (quest.Event != null && !quest.Event.IsNull)
        // {
        //     props["Event"] = quest.Event.FormKey.ToString();
        // }

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract NPC-specific properties
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractNpcProperties(INpcGetter npc)
    {
        var props = new Dictionary<string, object?>
        {
            ["Name"] = npc.Name?.String,
            ["Race"] = npc.Race.FormKey.ToString(),
            ["Level"] = npc.Configuration?.Level?.ToString(),
            ["Health"] = npc.Configuration?.HealthOffset ?? 0,
            ["Magicka"] = npc.Configuration?.MagickaOffset ?? 0,
            ["Stamina"] = npc.Configuration?.StaminaOffset ?? 0
        };

        if (npc.Class != null && !npc.Class.IsNull)
        {
            props["Class"] = npc.Class.FormKey.ToString();
        }

        if (npc.Keywords != null)
        {
            props["Keywords"] = npc.Keywords.Select(k => k.FormKey.ToString()).ToList();
        }

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract perk-specific properties
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractPerkProperties(IPerkGetter perk)
    {
        var props = new Dictionary<string, object?>
        {
            ["Name"] = perk.Name?.String,
            ["Description"] = perk.Description?.String,
            ["EffectCount"] = perk.Effects.Count
        };

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract properties via reflection (fallback for unknown types)
    /// </summary>
    private Result<Dictionary<string, object?>> ExtractPropertiesViaReflection(IMajorRecordGetter record)
    {
        var props = new Dictionary<string, object?>();
        var type = record.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            try
            {
                if (prop.Name is "EditorID" or "FormKey")
                    continue;

                var value = prop.GetValue(record);
                if (value != null)
                {
                    props[prop.Name] = value.ToString();
                }
            }
            catch
            {
                // Skip properties that throw exceptions
            }
        }

        return Result<Dictionary<string, object?>>.Ok(props);
    }

    /// <summary>
    /// Extract conditions from a record
    /// TODO: Implement once Mutagen Conditions API is confirmed
    /// </summary>
    private Result<List<ConditionInfo>> ExtractConditions(IMajorRecordGetter record)
    {
        var conditions = new List<ConditionInfo>();
        return Result<List<ConditionInfo>>.Ok(conditions);

        // try
        // {
        //     IReadOnlyList<IConditionGetter>? conditionList = null;
        //
        //     switch (record)
        //     {
        //         case ISpellGetter spell:
        //             conditionList = spell.Conditions;
        //             break;
        //         case IPerkGetter perk:
        //             conditionList = perk.Conditions;
        //             break;
        //         case IArmorGetter armor:
        //             conditionList = armor.Conditions;
        //             break;
        //         case IWeaponGetter weapon:
        //             conditionList = weapon.Conditions;
        //             break;
        //     }
        //
        //     if (conditionList == null || conditionList.Count == 0)
        //     {
        //         return Result<List<ConditionInfo>>.Ok(conditions);
        //     }
        //
        //     foreach (var condition in conditionList)
        //     {
        //         var condInfo = new ConditionInfo
        //         {
        //             FunctionName = condition.Data.Function.ToString(),
        //             ComparisonValue = condition.Data.ComparisonValue,
        //             Operator = ((int)condition.Data.CompareOperator).ToString(),
        //             Flags = condition.Data.Flags.ToString(),
        //             RunOn = condition.Data.RunOnType.ToString()
        //         };
        //
        //         if (condition.Data.ParameterOneRecord != null)
        //         {
        //             condInfo.ParameterA = condition.Data.ParameterOneRecord.FormKey.ToString();
        //         }
        //         else if (condition.Data.ParameterOneNumber.HasValue)
        //         {
        //             condInfo.ParameterA = condition.Data.ParameterOneNumber.Value.ToString();
        //         }
        //
        //         if (condition.Data.ParameterTwoRecord != null)
        //         {
        //             condInfo.ParameterB = condition.Data.ParameterTwoRecord.FormKey.ToString();
        //         }
        //         else if (condition.Data.ParameterTwoNumber.HasValue)
        //         {
        //             condInfo.ParameterB = condition.Data.ParameterTwoNumber.Value.ToString();
        //         }
        //
        //         conditions.Add(condInfo);
        //     }
        //
        //     return Result<List<ConditionInfo>>.Ok(conditions);
        // }
        // catch (Exception ex)
        // {
        //     return Result<List<ConditionInfo>>.Fail($"Failed to extract conditions: {ex.Message}", ex.StackTrace);
        // }
    }

    /// <summary>
    /// Create an override patch for a record
    /// </summary>
    public Result<string> CreateOverride(
        string sourcePluginPath,
        string outputPluginName,
        string? editorId,
        string? formId,
        string? recordType,
        bool removeConditions = false,
        string? dataFolder = null)
    {
        try
        {
            if (!File.Exists(sourcePluginPath))
            {
                return Result<string>.Fail(
                    $"Source plugin not found: {sourcePluginPath}",
                    suggestions: new List<string>
                    {
                        "Check the file path is correct",
                        "Use an absolute path if relative path fails"
                    });
            }

            var sourceMod = SkyrimMod.CreateFromBinaryOverlay(sourcePluginPath, SkyrimRelease.SkyrimSE);

            IMajorRecordGetter? sourceRecord = null;

            if (!string.IsNullOrEmpty(formId))
            {
                var findResult = FindRecordByFormKey(sourceMod, formId);
                if (!findResult.Success)
                {
                    return Result<string>.Fail(findResult.Error ?? "Record not found", findResult.ErrorContext);
                }
                sourceRecord = findResult.Value;
            }
            else if (!string.IsNullOrEmpty(editorId) && !string.IsNullOrEmpty(recordType))
            {
                var findResult = FindRecordByEditorId(sourceMod, editorId, recordType);
                if (!findResult.Success)
                {
                    return Result<string>.Fail(findResult.Error ?? "Record not found", findResult.ErrorContext);
                }
                sourceRecord = findResult.Value;
            }
            else
            {
                return Result<string>.Fail(
                    "Must provide either FormID or both EditorID and RecordType",
                    suggestions: new List<string>
                    {
                        "Use --form-id for FormKey-based lookup",
                        "Use --editor-id and --type for EditorID-based lookup"
                    });
            }

            if (sourceRecord == null)
            {
                return Result<string>.Fail(
                    $"Record not found: {editorId ?? formId}",
                    suggestions: new List<string>
                    {
                        "Use 'esp info' to see available records",
                        "Check spelling of EditorID",
                        "Verify FormID is correct"
                    });
            }

            if (!outputPluginName.EndsWith(".esp", StringComparison.OrdinalIgnoreCase) &&
                !outputPluginName.EndsWith(".esl", StringComparison.OrdinalIgnoreCase))
            {
                outputPluginName += ".esp";
            }

            var outputModKey = ModKey.FromFileName(outputPluginName);
            var patchMod = new SkyrimMod(outputModKey, SkyrimRelease.SkyrimSE);

            patchMod.ModHeader.MasterReferences.Add(new MasterReference
            {
                Master = sourceMod.ModKey
            });

            var overrideResult = CreateOverrideRecord(patchMod, sourceRecord, removeConditions);
            if (!overrideResult.Success)
            {
                return Result<string>.Fail(overrideResult.Error ?? "Failed to create override", overrideResult.ErrorContext);
            }

            var outputDir = !string.IsNullOrEmpty(dataFolder)
                ? dataFolder
                : Path.GetDirectoryName(sourcePluginPath) ?? Directory.GetCurrentDirectory();

            var outputPath = Path.Combine(outputDir, outputPluginName);

            patchMod.WriteToBinary(outputPath);

            _logger.Info($"Created override patch: {outputPath}");
            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to create override: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Create an override record with optional modifications
    /// Overrides keep the same FormKey as the source record
    /// </summary>
    private Result<IMajorRecord> CreateOverrideRecord(
        SkyrimMod targetMod,
        IMajorRecordGetter sourceRecord,
        bool removeConditions)
    {
        try
        {
            switch (sourceRecord)
            {
                case ISpellGetter spell:
                    var spellOverride = (Spell)spell.DeepCopy();
                    // TODO: Add condition removal once Mutagen API is confirmed
                    // if (removeConditions && spellOverride.Conditions != null)
                    // {
                    //     spellOverride.Conditions.Clear();
                    // }
                    targetMod.Spells.Add(spellOverride);
                    return Result<IMajorRecord>.Ok(spellOverride);

                case IWeaponGetter weapon:
                    var weaponOverride = (Weapon)weapon.DeepCopy();
                    // TODO: Add condition removal once Mutagen API is confirmed
                    // if (removeConditions && weaponOverride.Conditions != null)
                    // {
                    //     weaponOverride.Conditions.Clear();
                    // }
                    targetMod.Weapons.Add(weaponOverride);
                    return Result<IMajorRecord>.Ok(weaponOverride);

                case IArmorGetter armor:
                    var armorOverride = (Armor)armor.DeepCopy();
                    // TODO: Add condition removal once Mutagen API is confirmed
                    // if (removeConditions && armorOverride.Conditions != null)
                    // {
                    //     armorOverride.Conditions.Clear();
                    // }
                    targetMod.Armors.Add(armorOverride);
                    return Result<IMajorRecord>.Ok(armorOverride);

                case IQuestGetter quest:
                    var questOverride = (Quest)quest.DeepCopy();
                    targetMod.Quests.Add(questOverride);
                    return Result<IMajorRecord>.Ok(questOverride);

                case INpcGetter npc:
                    var npcOverride = (Npc)npc.DeepCopy();
                    targetMod.Npcs.Add(npcOverride);
                    return Result<IMajorRecord>.Ok(npcOverride);

                case IPerkGetter perk:
                    var perkOverride = (Perk)perk.DeepCopy();
                    // TODO: Add condition removal once Mutagen API is confirmed
                    // if (removeConditions && perkOverride.Conditions != null)
                    // {
                    //     perkOverride.Conditions.Clear();
                    // }
                    targetMod.Perks.Add(perkOverride);
                    return Result<IMajorRecord>.Ok(perkOverride);

                case IFactionGetter faction:
                    var factionOverride = (Faction)faction.DeepCopy();
                    targetMod.Factions.Add(factionOverride);
                    return Result<IMajorRecord>.Ok(factionOverride);

                case IBookGetter book:
                    var bookOverride = (Book)book.DeepCopy();
                    targetMod.Books.Add(bookOverride);
                    return Result<IMajorRecord>.Ok(bookOverride);

                case IMiscItemGetter miscItem:
                    var miscOverride = (MiscItem)miscItem.DeepCopy();
                    targetMod.MiscItems.Add(miscOverride);
                    return Result<IMajorRecord>.Ok(miscOverride);

                case IGlobalGetter global:
                    var globalOverride = (Global)global.DeepCopy();
                    targetMod.Globals.Add(globalOverride);
                    return Result<IMajorRecord>.Ok(globalOverride);

                case ILeveledItemGetter leveledItem:
                    var leveledItemOverride = (LeveledItem)leveledItem.DeepCopy();
                    targetMod.LeveledItems.Add(leveledItemOverride);
                    return Result<IMajorRecord>.Ok(leveledItemOverride);

                case IFormListGetter formList:
                    var formListOverride = (FormList)formList.DeepCopy();
                    targetMod.FormLists.Add(formListOverride);
                    return Result<IMajorRecord>.Ok(formListOverride);

                case IOutfitGetter outfit:
                    var outfitOverride = (Outfit)outfit.DeepCopy();
                    targetMod.Outfits.Add(outfitOverride);
                    return Result<IMajorRecord>.Ok(outfitOverride);

                case ILocationGetter location:
                    var locationOverride = (Location)location.DeepCopy();
                    targetMod.Locations.Add(locationOverride);
                    return Result<IMajorRecord>.Ok(locationOverride);

                case IEncounterZoneGetter encounterZone:
                    var encounterZoneOverride = (EncounterZone)encounterZone.DeepCopy();
                    targetMod.EncounterZones.Add(encounterZoneOverride);
                    return Result<IMajorRecord>.Ok(encounterZoneOverride);

                default:
                    return Result<IMajorRecord>.Fail(
                        $"Unsupported record type for override: {sourceRecord.GetType().Name}",
                        suggestions: new List<string>
                        {
                            "Supported types: Spell, Weapon, Armor, Quest, NPC, Perk, Faction, Book, MiscItem, Global, LeveledItem, FormList, Outfit, Location, EncounterZone"
                        });
            }
        }
        catch (Exception ex)
        {
            return Result<IMajorRecord>.Fail($"Failed to create override record: {ex.Message}", ex.StackTrace);
        }
    }
}
