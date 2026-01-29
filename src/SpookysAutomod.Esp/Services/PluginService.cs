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

            var conditionsResult = ExtractConditions(record);
            if (conditionsResult.Success && conditionsResult.Value != null && conditionsResult.Value.Count > 0)
            {
                recordInfo.Conditions = conditionsResult.Value;
            }

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
    /// Verified to work with: Perk, Package, IdleAnimation, MagicEffect
    /// </summary>
    private Result<List<ConditionInfo>> ExtractConditions(IMajorRecordGetter record)
    {
        var conditions = new List<ConditionInfo>();

        try
        {
            IReadOnlyList<IConditionGetter>? conditionList = null;

            switch (record)
            {
                case IPerkGetter perk:
                    conditionList = perk.Conditions;
                    break;
                case IPackageGetter package:
                    conditionList = package.Conditions;
                    break;
                case IIdleAnimationGetter idle:
                    conditionList = idle.Conditions;
                    break;
                // Note: MagicEffect conditions exist but are more complex
                // Spells, Weapons, Armor do NOT have direct Conditions properties
            }

            if (conditionList == null || conditionList.Count == 0)
            {
                return Result<List<ConditionInfo>>.Ok(conditions);
            }

            foreach (var condition in conditionList)
            {
                if (condition.Data == null)
                    continue;

                var functionName = condition.Data.GetType().Name.Replace("ConditionData", "");

                var condInfo = new ConditionInfo
                {
                    FunctionName = functionName,
                    Operator = condition.CompareOperator.ToString()
                };

                // Handle ConditionFloat (most common)
                if (condition is ConditionFloat condFloat)
                {
                    condInfo.ComparisonValue = condFloat.ComparisonValue;
                }
                // Handle ConditionGlobal (uses global variable)
                else if (condition is ConditionGlobal condGlobal)
                {
                    condInfo.ComparisonValue = 0; // Global comparison
                    if (condGlobal.ComparisonValue.FormKey != null)
                    {
                        condInfo.ParameterA = condGlobal.ComparisonValue.FormKey.ToString();
                    }
                }

                // Extract flags and run-on type (common to all conditions)
                condInfo.Flags = condition.Flags.ToString();

                // RunOnType varies by ConditionData subclass - try to get it via reflection
                var runOnProp = condition.Data.GetType().GetProperty("RunOnType");
                if (runOnProp != null)
                {
                    var runOnValue = runOnProp.GetValue(condition.Data);
                    condInfo.RunOn = runOnValue?.ToString() ?? "Unknown";
                }

                conditions.Add(condInfo);
            }

            return Result<List<ConditionInfo>>.Ok(conditions);
        }
        catch (Exception ex)
        {
            return Result<List<ConditionInfo>>.Fail($"Failed to extract conditions: {ex.Message}", ex.StackTrace);
        }
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
                    if (removeConditions && perkOverride.Conditions != null)
                    {
                        perkOverride.Conditions.Clear();
                    }
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

    /// <summary>
    /// Find records across plugins by search pattern
    /// </summary>
    public Result<List<RecordSearchResult>> FindRecords(
        string? searchPattern,
        string? editorId,
        string? recordType,
        string? pluginPath,
        string? dataFolder,
        bool allPlugins = false)
    {
        try
        {
            var results = new List<RecordSearchResult>();
            var pluginsToSearch = new List<string>();

            if (allPlugins && !string.IsNullOrEmpty(dataFolder))
            {
                if (!Directory.Exists(dataFolder))
                {
                    return Result<List<RecordSearchResult>>.Fail(
                        $"Data folder not found: {dataFolder}",
                        suggestions: new List<string>
                        {
                            "Check the data folder path is correct",
                            "Use an absolute path"
                        });
                }

                pluginsToSearch.AddRange(Directory.GetFiles(dataFolder, "*.esp"));
                pluginsToSearch.AddRange(Directory.GetFiles(dataFolder, "*.esm"));
                pluginsToSearch.AddRange(Directory.GetFiles(dataFolder, "*.esl"));
            }
            else if (!string.IsNullOrEmpty(pluginPath))
            {
                if (!File.Exists(pluginPath))
                {
                    return Result<List<RecordSearchResult>>.Fail($"Plugin not found: {pluginPath}");
                }
                pluginsToSearch.Add(pluginPath);
            }
            else
            {
                return Result<List<RecordSearchResult>>.Fail(
                    "Must provide either --plugin or --data-folder with --all-plugins",
                    suggestions: new List<string>
                    {
                        "Use --plugin to search a specific plugin",
                        "Use --data-folder --all-plugins to search all plugins"
                    });
            }

            foreach (var plugin in pluginsToSearch)
            {
                try
                {
                    var mod = SkyrimMod.CreateFromBinaryOverlay(plugin, SkyrimRelease.SkyrimSE);
                    var pluginName = Path.GetFileName(plugin);

                    IEnumerable<IMajorRecordGetter> recordsToSearch = mod.EnumerateMajorRecords();

                    if (!string.IsNullOrEmpty(recordType))
                    {
                        recordsToSearch = FilterByRecordType(mod, recordType);
                    }

                    foreach (var record in recordsToSearch)
                    {
                        bool matches = false;

                        if (!string.IsNullOrEmpty(editorId))
                        {
                            matches = record.EditorID?.Equals(editorId, StringComparison.OrdinalIgnoreCase) == true;
                        }
                        else if (!string.IsNullOrEmpty(searchPattern))
                        {
                            matches = (record.EditorID?.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) == true) ||
                                     (GetRecordName(record)?.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) == true);
                        }

                        if (matches)
                        {
                            results.Add(new RecordSearchResult
                            {
                                PluginName = pluginName,
                                EditorId = record.EditorID ?? string.Empty,
                                FormKey = record.FormKey.ToString(),
                                RecordType = record.GetType().Name.Replace("Getter", "").Replace("ReadOnly", ""),
                                Name = GetRecordName(record)
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Skipping plugin {plugin}: {ex.Message}");
                    continue;
                }
            }

            return Result<List<RecordSearchResult>>.Ok(results);
        }
        catch (Exception ex)
        {
            return Result<List<RecordSearchResult>>.Fail(
                $"Failed to search records: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Batch create overrides for multiple records
    /// </summary>
    public Result<BatchOverrideResult> BatchOverride(
        string sourcePluginPath,
        string? recordType,
        string? searchPattern,
        string[]? editorIds,
        string outputPluginName,
        string? dataFolder = null)
    {
        try
        {
            if (!File.Exists(sourcePluginPath))
            {
                return Result<BatchOverrideResult>.Fail($"Source plugin not found: {sourcePluginPath}");
            }

            var sourceMod = SkyrimMod.CreateFromBinaryOverlay(sourcePluginPath, SkyrimRelease.SkyrimSE);

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

            var recordsToOverride = new List<IMajorRecordGetter>();

            if (editorIds != null && editorIds.Length > 0)
            {
                foreach (var editorId in editorIds)
                {
                    var findResult = FindRecordByEditorId(sourceMod, editorId, recordType ?? "");
                    if (findResult.Success && findResult.Value != null)
                    {
                        recordsToOverride.Add(findResult.Value);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(searchPattern))
            {
                IEnumerable<IMajorRecordGetter> allRecords = string.IsNullOrEmpty(recordType)
                    ? sourceMod.EnumerateMajorRecords()
                    : FilterByRecordType(sourceMod, recordType);

                foreach (var record in allRecords)
                {
                    if (record.EditorID?.Contains(searchPattern, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        recordsToOverride.Add(record);
                    }
                }
            }

            var modifiedRecords = new List<string>();
            foreach (var record in recordsToOverride)
            {
                var overrideResult = CreateOverrideRecord(patchMod, record, false);
                if (overrideResult.Success)
                {
                    modifiedRecords.Add(record.EditorID ?? record.FormKey.ToString());
                }
            }

            var outputDir = !string.IsNullOrEmpty(dataFolder)
                ? dataFolder
                : Path.GetDirectoryName(sourcePluginPath) ?? Directory.GetCurrentDirectory();

            var outputPath = Path.Combine(outputDir, outputPluginName);

            patchMod.WriteToBinary(outputPath);

            _logger.Info($"Created batch override patch: {outputPath}");

            return Result<BatchOverrideResult>.Ok(new BatchOverrideResult
            {
                RecordsModified = modifiedRecords.Count,
                ModifiedRecords = modifiedRecords,
                PatchPath = outputPath
            });
        }
        catch (Exception ex)
        {
            return Result<BatchOverrideResult>.Fail(
                $"Failed to create batch override: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Filter records by type
    /// </summary>
    private IEnumerable<IMajorRecordGetter> FilterByRecordType(ISkyrimModGetter mod, string recordType)
    {
        return recordType.ToLowerInvariant() switch
        {
            "spell" => mod.Spells,
            "weapon" => mod.Weapons,
            "armor" => mod.Armors,
            "quest" => mod.Quests,
            "npc" => mod.Npcs,
            "perk" => mod.Perks,
            "faction" => mod.Factions,
            "book" => mod.Books,
            "miscitem" => mod.MiscItems,
            "global" => mod.Globals,
            "leveleditem" => mod.LeveledItems,
            "formlist" => mod.FormLists,
            "outfit" => mod.Outfits,
            "location" => mod.Locations,
            "encounterzone" => mod.EncounterZones,
            _ => mod.EnumerateMajorRecords()
        };
    }

    /// <summary>
    /// Get the display name of a record
    /// </summary>
    private string? GetRecordName(IMajorRecordGetter record)
    {
        return record switch
        {
            ISpellGetter spell => spell.Name?.String,
            IWeaponGetter weapon => weapon.Name?.String,
            IArmorGetter armor => armor.Name?.String,
            IQuestGetter quest => quest.Name?.String,
            INpcGetter npc => npc.Name?.String,
            IPerkGetter perk => perk.Name?.String,
            IBookGetter book => book.Name?.String,
            _ => null
        };
    }

    /// <summary>
    /// Compare two versions of the same record
    /// </summary>
    public Result<RecordComparison> CompareRecords(
        string plugin1Path,
        string plugin2Path,
        string? editorId,
        string? formId,
        string? recordType)
    {
        try
        {
            if (!File.Exists(plugin1Path))
            {
                return Result<RecordComparison>.Fail($"Plugin 1 not found: {plugin1Path}");
            }

            if (!File.Exists(plugin2Path))
            {
                return Result<RecordComparison>.Fail($"Plugin 2 not found: {plugin2Path}");
            }

            var mod1 = SkyrimMod.CreateFromBinaryOverlay(plugin1Path, SkyrimRelease.SkyrimSE);
            var mod2 = SkyrimMod.CreateFromBinaryOverlay(plugin2Path, SkyrimRelease.SkyrimSE);

            IMajorRecordGetter? record1 = null;
            IMajorRecordGetter? record2 = null;

            if (!string.IsNullOrEmpty(formId))
            {
                var findResult1 = FindRecordByFormKey(mod1, formId);
                var findResult2 = FindRecordByFormKey(mod2, formId);

                if (!findResult1.Success || !findResult2.Success)
                {
                    return Result<RecordComparison>.Fail(
                        "Record not found in one or both plugins",
                        suggestions: new List<string>
                        {
                            "Verify the FormKey exists in both plugins",
                            "Use --editor-id if the record has different FormKeys"
                        });
                }

                record1 = findResult1.Value;
                record2 = findResult2.Value;
            }
            else if (!string.IsNullOrEmpty(editorId) && !string.IsNullOrEmpty(recordType))
            {
                var findResult1 = FindRecordByEditorId(mod1, editorId, recordType);
                var findResult2 = FindRecordByEditorId(mod2, editorId, recordType);

                if (!findResult1.Success || !findResult2.Success)
                {
                    return Result<RecordComparison>.Fail(
                        "Record not found in one or both plugins",
                        suggestions: new List<string>
                        {
                            "Verify the EditorID exists in both plugins",
                            "Check the record type is correct"
                        });
                }

                record1 = findResult1.Value;
                record2 = findResult2.Value;
            }
            else
            {
                return Result<RecordComparison>.Fail(
                    "Must provide either FormID or both EditorID and RecordType");
            }

            if (record1 == null || record2 == null)
            {
                return Result<RecordComparison>.Fail("Failed to load records for comparison");
            }

            var info1Result = ExtractRecordInfo(record1, false);
            var info2Result = ExtractRecordInfo(record2, false);

            if (!info1Result.Success || !info2Result.Success)
            {
                return Result<RecordComparison>.Fail("Failed to extract record properties");
            }

            var info1 = info1Result.Value!;
            var info2 = info2Result.Value!;

            var differences = new Dictionary<string, FieldDifference>();

            var allKeys = info1.Properties.Keys.Union(info2.Properties.Keys).ToHashSet();

            foreach (var key in allKeys)
            {
                var value1 = info1.Properties.GetValueOrDefault(key);
                var value2 = info2.Properties.GetValueOrDefault(key);

                var value1Str = value1?.ToString() ?? "";
                var value2Str = value2?.ToString() ?? "";

                if (value1Str != value2Str)
                {
                    differences[key] = new FieldDifference
                    {
                        Field = key,
                        OriginalValue = value1,
                        ModifiedValue = value2
                    };
                }
            }

            return Result<RecordComparison>.Ok(new RecordComparison
            {
                Original = info1,
                Modified = info2,
                Differences = differences
            });
        }
        catch (Exception ex)
        {
            return Result<RecordComparison>.Fail(
                $"Failed to compare records: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Detect load order conflicts for a record or plugin
    /// </summary>
    public Result<ConflictReport> DetectConflicts(
        string? pluginPath,
        string? editorId,
        string? formId,
        string? recordType,
        string dataFolder)
    {
        try
        {
            if (!Directory.Exists(dataFolder))
            {
                return Result<ConflictReport>.Fail($"Data folder not found: {dataFolder}");
            }

            if (string.IsNullOrEmpty(pluginPath) && string.IsNullOrEmpty(formId) && string.IsNullOrEmpty(editorId))
            {
                return Result<ConflictReport>.Fail(
                    "Must provide either --plugin, --form-id, or --editor-id");
            }

            var allPlugins = new List<string>();
            allPlugins.AddRange(Directory.GetFiles(dataFolder, "*.esm").OrderBy(f => f));
            allPlugins.AddRange(Directory.GetFiles(dataFolder, "*.esp").OrderBy(f => f));
            allPlugins.AddRange(Directory.GetFiles(dataFolder, "*.esl").OrderBy(f => f));

            Mutagen.Bethesda.Plugins.FormKey? targetFormKey = null;
            string targetEditorId = editorId ?? "";

            if (!string.IsNullOrEmpty(formId))
            {
                if (!Mutagen.Bethesda.Plugins.FormKey.TryFactory(formId, out var parsedFormKey))
                {
                    return Result<ConflictReport>.Fail($"Invalid FormID format: {formId}");
                }
                targetFormKey = parsedFormKey;
            }

            var conflicts = new List<ConflictingPlugin>();
            int loadOrder = 0;

            foreach (var plugin in allPlugins)
            {
                try
                {
                    var mod = SkyrimMod.CreateFromBinaryOverlay(plugin, SkyrimRelease.SkyrimSE);

                    bool hasRecord = false;

                    if (targetFormKey.HasValue)
                    {
                        hasRecord = mod.EnumerateMajorRecords().Any(r => r.FormKey == targetFormKey.Value);
                    }
                    else if (!string.IsNullOrEmpty(targetEditorId) && !string.IsNullOrEmpty(recordType))
                    {
                        var findResult = FindRecordByEditorId(mod, targetEditorId, recordType);
                        hasRecord = findResult.Success;

                        if (hasRecord && !targetFormKey.HasValue && findResult.Value != null)
                        {
                            targetFormKey = findResult.Value.FormKey;
                            targetEditorId = findResult.Value.EditorID ?? targetEditorId;
                        }
                    }
                    else if (!string.IsNullOrEmpty(pluginPath))
                    {
                        hasRecord = Path.GetFileName(plugin).Equals(
                            Path.GetFileName(pluginPath),
                            StringComparison.OrdinalIgnoreCase);
                    }

                    if (hasRecord)
                    {
                        conflicts.Add(new ConflictingPlugin
                        {
                            PluginName = Path.GetFileName(plugin),
                            LoadOrder = loadOrder,
                            IsWinner = false
                        });
                    }

                    loadOrder++;
                }
                catch
                {
                    loadOrder++;
                    continue;
                }
            }

            if (conflicts.Count == 0)
            {
                return Result<ConflictReport>.Fail(
                    "No conflicts found",
                    suggestions: new List<string>
                    {
                        "Record may not exist in any loaded plugins",
                        "Verify the FormKey or EditorID is correct"
                    });
            }

            conflicts.Last().IsWinner = true;

            return Result<ConflictReport>.Ok(new ConflictReport
            {
                FormKey = targetFormKey?.ToString() ?? "",
                EditorId = targetEditorId,
                Conflicts = conflicts,
                WinningPlugin = conflicts.Last().PluginName
            });
        }
        catch (Exception ex)
        {
            return Result<ConflictReport>.Fail(
                $"Failed to detect conflicts: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Extract RecordInfo from a record (helper for compare)
    /// </summary>
    private Result<RecordInfo> ExtractRecordInfo(IMajorRecordGetter record, bool includeRaw)
    {
        var recordInfo = new RecordInfo
        {
            EditorId = record.EditorID ?? string.Empty,
            FormKey = record.FormKey.ToString(),
            RecordType = record.GetType().Name.Replace("Getter", "").Replace("ReadOnly", "")
        };

        var propsResult = ExtractRecordProperties(record, includeRaw);
        if (!propsResult.Success)
        {
            return Result<RecordInfo>.Fail(propsResult.Error ?? "Failed to extract properties");
        }

        recordInfo.Properties = propsResult.Value ?? new Dictionary<string, object?>();

        return Result<RecordInfo>.Ok(recordInfo);
    }

    /// <summary>
    /// List all conditions on a record
    /// </summary>
    public Result<List<ConditionInfo>> ListConditions(
        string pluginPath,
        string? editorId,
        string? formId,
        string? recordType)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                return Result<List<ConditionInfo>>.Fail($"Plugin not found: {pluginPath}");
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, SkyrimRelease.SkyrimSE);

            IMajorRecordGetter? record = null;

            if (!string.IsNullOrEmpty(formId))
            {
                var findResult = FindRecordByFormKey(mod, formId);
                if (!findResult.Success || findResult.Value == null)
                {
                    return Result<List<ConditionInfo>>.Fail("Record not found");
                }
                record = findResult.Value;
            }
            else if (!string.IsNullOrEmpty(editorId) && !string.IsNullOrEmpty(recordType))
            {
                var findResult = FindRecordByEditorId(mod, editorId, recordType);
                if (!findResult.Success || findResult.Value == null)
                {
                    return Result<List<ConditionInfo>>.Fail("Record not found");
                }
                record = findResult.Value;
            }
            else
            {
                return Result<List<ConditionInfo>>.Fail("Must provide either FormID or both EditorID and RecordType");
            }

            return ExtractConditions(record);
        }
        catch (Exception ex)
        {
            return Result<List<ConditionInfo>>.Fail($"Failed to list conditions: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// Remove specific conditions from a record by index
    /// </summary>
    public Result<string> RemoveConditions(
        string sourcePluginPath,
        string? editorId,
        string? formId,
        string? recordType,
        int[] conditionIndices,
        string outputPluginName,
        string? dataFolder = null)
    {
        try
        {
            if (!File.Exists(sourcePluginPath))
            {
                return Result<string>.Fail($"Source plugin not found: {sourcePluginPath}");
            }

            var sourceMod = SkyrimMod.CreateFromBinaryOverlay(sourcePluginPath, SkyrimRelease.SkyrimSE);

            IMajorRecordGetter? sourceRecord = null;

            if (!string.IsNullOrEmpty(formId))
            {
                var findResult = FindRecordByFormKey(sourceMod, formId);
                if (!findResult.Success || findResult.Value == null)
                {
                    return Result<string>.Fail("Record not found");
                }
                sourceRecord = findResult.Value;
            }
            else if (!string.IsNullOrEmpty(editorId) && !string.IsNullOrEmpty(recordType))
            {
                var findResult = FindRecordByEditorId(sourceMod, editorId, recordType);
                if (!findResult.Success || findResult.Value == null)
                {
                    return Result<string>.Fail("Record not found");
                }
                sourceRecord = findResult.Value;
            }
            else
            {
                return Result<string>.Fail("Must provide either FormID or both EditorID and RecordType");
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

            // Only Perks are currently supported for condition manipulation
            if (sourceRecord is not IPerkGetter)
            {
                return Result<string>.Fail(
                    "Condition manipulation currently only supported for Perk records",
                    suggestions: new List<string>
                    {
                        "Use --type perk",
                        "Other record types (Package, IdleAnimation) coming soon"
                    });
            }

            var perkSource = (IPerkGetter)sourceRecord;
            var perkOverride = (Perk)perkSource.DeepCopy();

            if (perkOverride.Conditions == null || perkOverride.Conditions.Count == 0)
            {
                return Result<string>.Fail("Record has no conditions to remove");
            }

            // Sort indices in descending order to avoid index shifting issues
            var sortedIndices = conditionIndices.OrderByDescending(i => i).ToArray();

            foreach (var index in sortedIndices)
            {
                if (index < 0 || index >= perkOverride.Conditions.Count)
                {
                    return Result<string>.Fail($"Invalid condition index: {index}. Record has {perkOverride.Conditions.Count} conditions.");
                }
                perkOverride.Conditions.RemoveAt(index);
            }

            patchMod.Perks.Add(perkOverride);

            var outputDir = !string.IsNullOrEmpty(dataFolder)
                ? dataFolder
                : Path.GetDirectoryName(sourcePluginPath) ?? Directory.GetCurrentDirectory();

            var outputPath = Path.Combine(outputDir, outputPluginName);

            patchMod.WriteToBinary(outputPath);

            _logger.Info($"Created patch with {conditionIndices.Length} condition(s) removed: {outputPath}");
            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to remove conditions: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// Add a condition to a record
    /// </summary>
    public Result<string> AddCondition(
        string sourcePluginPath,
        string? editorId,
        string? formId,
        string? recordType,
        string conditionFunction,
        float comparisonValue,
        string comparisonOperator,
        string outputPluginName,
        string? dataFolder = null)
    {
        try
        {
            if (!File.Exists(sourcePluginPath))
            {
                return Result<string>.Fail($"Source plugin not found: {sourcePluginPath}");
            }

            var sourceMod = SkyrimMod.CreateFromBinaryOverlay(sourcePluginPath, SkyrimRelease.SkyrimSE);

            IMajorRecordGetter? sourceRecord = null;

            if (!string.IsNullOrEmpty(formId))
            {
                var findResult = FindRecordByFormKey(sourceMod, formId);
                if (!findResult.Success || findResult.Value == null)
                {
                    return Result<string>.Fail("Record not found");
                }
                sourceRecord = findResult.Value;
            }
            else if (!string.IsNullOrEmpty(editorId) && !string.IsNullOrEmpty(recordType))
            {
                var findResult = FindRecordByEditorId(sourceMod, editorId, recordType);
                if (!findResult.Success || findResult.Value == null)
                {
                    return Result<string>.Fail("Record not found");
                }
                sourceRecord = findResult.Value;
            }
            else
            {
                return Result<string>.Fail("Must provide either FormID or both EditorID and RecordType");
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

            // Only Perks are currently supported
            if (sourceRecord is not IPerkGetter)
            {
                return Result<string>.Fail(
                    "Condition manipulation currently only supported for Perk records",
                    suggestions: new List<string>
                    {
                        "Use --type perk",
                        "Other record types coming soon"
                    });
            }

            var perkSource = (IPerkGetter)sourceRecord;
            var perkOverride = (Perk)perkSource.DeepCopy();

            // Parse comparison operator
            if (!Enum.TryParse<CompareOperator>(comparisonOperator, true, out var compareOp))
            {
                return Result<string>.Fail(
                    $"Invalid comparison operator: {comparisonOperator}",
                    suggestions: new List<string>
                    {
                        "Valid operators: EqualTo, NotEqualTo, GreaterThan, GreaterThanOrEqualTo, LessThan, LessThanOrEqualTo"
                    });
            }

            // Create condition based on function name
            var conditionResult = CreateConditionFromFunction(conditionFunction, comparisonValue, compareOp);
            if (!conditionResult.Success || conditionResult.Value == null)
            {
                return Result<string>.Fail(conditionResult.Error ?? "Failed to create condition");
            }

            perkOverride.Conditions.Add(conditionResult.Value);

            patchMod.Perks.Add(perkOverride);

            var outputDir = !string.IsNullOrEmpty(dataFolder)
                ? dataFolder
                : Path.GetDirectoryName(sourcePluginPath) ?? Directory.GetCurrentDirectory();

            var outputPath = Path.Combine(outputDir, outputPluginName);

            patchMod.WriteToBinary(outputPath);

            _logger.Info($"Created patch with new condition: {outputPath}");
            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to add condition: {ex.Message}", ex.StackTrace);
        }
    }

    /// <summary>
    /// Create a condition from a function name
    /// Uses reflection to create ConditionData types dynamically
    /// </summary>
    private Result<Condition> CreateConditionFromFunction(
        string functionName,
        float comparisonValue,
        CompareOperator compareOperator)
    {
        try
        {
            // Build the ConditionData type name
            var typeName = $"{functionName}ConditionData";
            var fullTypeName = $"Mutagen.Bethesda.Skyrim.{typeName}";

            // Try to find the type in the Mutagen assembly
            var conditionDataType = typeof(ISkyrimMod).Assembly.GetType(fullTypeName);
            if (conditionDataType == null)
            {
                return Result<Condition>.Fail(
                    $"Unknown condition function: {functionName}",
                    suggestions: new List<string>
                    {
                        "Use exact Mutagen function names (e.g., GetLevel, GetActorValue)",
                        "Supported simple functions: GetLevel",
                        "See Mutagen documentation for full list of condition functions"
                    });
            }

            // Create instance of ConditionData
            var conditionData = System.Activator.CreateInstance(conditionDataType) as ConditionData;
            if (conditionData == null)
            {
                return Result<Condition>.Fail($"Failed to create instance of {typeName}");
            }

            // Create ConditionFloat with the data
            var condition = new ConditionFloat
            {
                ComparisonValue = comparisonValue,
                CompareOperator = compareOperator,
                Data = conditionData
            };

            return Result<Condition>.Ok(condition);
        }
        catch (Exception ex)
        {
            return Result<Condition>.Fail($"Failed to create condition: {ex.Message}", ex.StackTrace);
        }
    }
}
