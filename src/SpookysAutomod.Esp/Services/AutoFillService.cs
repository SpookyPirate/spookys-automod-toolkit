using System.Text.RegularExpressions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Service for automatically filling script properties from PSC files and Skyrim.esm.
/// </summary>
public class AutoFillService
{
    private readonly IModLogger _logger;
    private readonly LinkCacheManager _linkCacheManager;

    // Papyrus type to Mutagen interface mapping
    private static readonly Dictionary<string, Type[]> PapyrusTypeToRecordTypes = new()
    {
        ["Keyword"] = new[] { typeof(IKeywordGetter) },
        ["GlobalVariable"] = new[] { typeof(IGlobalGetter) },
        ["Quest"] = new[] { typeof(IQuestGetter) },
        ["Faction"] = new[] { typeof(IFactionGetter) },
        ["Actor"] = new[] { typeof(INpcGetter) },
        ["ActorBase"] = new[] { typeof(INpcGetter) },
        ["Spell"] = new[] { typeof(ISpellGetter) },
        ["Perk"] = new[] { typeof(IPerkGetter) },
        ["Weapon"] = new[] { typeof(IWeaponGetter) },
        ["Armor"] = new[] { typeof(IArmorGetter) },
        ["Book"] = new[] { typeof(IBookGetter) },
        ["Location"] = new[] { typeof(ILocationGetter) },
        ["WorldSpace"] = new[] { typeof(IWorldspaceGetter) },
        ["MagicEffect"] = new[] { typeof(IMagicEffectGetter) },
        ["Enchantment"] = new[] { typeof(IObjectEffectGetter) },
        ["FormList"] = new[] { typeof(IFormListGetter) },
        ["LeveledItem"] = new[] { typeof(ILeveledItemGetter) },
        ["LeveledActor"] = new[] { typeof(ILeveledNpcGetter) },
        ["LeveledSpell"] = new[] { typeof(ILeveledSpellGetter) },
        ["Sound"] = new[] { typeof(ISoundDescriptorGetter) },
        ["Static"] = new[] { typeof(IStaticGetter) },
        ["Activator"] = new[] { typeof(IActivatorGetter) },
        ["Container"] = new[] { typeof(IContainerGetter) },
        ["Key"] = new[] { typeof(IKeyGetter) },
        ["Potion"] = new[] { typeof(IIngestibleGetter) },
        ["Ingredient"] = new[] { typeof(IIngredientGetter) },
        ["Race"] = new[] { typeof(IRaceGetter) },
        ["Class"] = new[] { typeof(IClassGetter) },
        ["CombatStyle"] = new[] { typeof(ICombatStyleGetter) },
        ["EncounterZone"] = new[] { typeof(IEncounterZoneGetter) },
        ["VoiceType"] = new[] { typeof(IVoiceTypeGetter) },
        ["Furniture"] = new[] { typeof(IFurnitureGetter) },
        ["Package"] = new[] { typeof(IPackageGetter) },
        ["Idle"] = new[] { typeof(IIdleAnimationGetter) },
        ["Message"] = new[] { typeof(IMessageGetter) },
        ["Shout"] = new[] { typeof(IShoutGetter) },
        ["EffectShader"] = new[] { typeof(IEffectShaderGetter) },
        ["Explosion"] = new[] { typeof(IExplosionGetter) },
        ["ImageSpaceModifier"] = new[] { typeof(IImageSpaceAdapterGetter) },
        ["Hazard"] = new[] { typeof(IHazardGetter) },
        ["Scroll"] = new[] { typeof(IScrollGetter) },
        ["ArtObject"] = new[] { typeof(IArtObjectGetter) },
        ["Projectile"] = new[] { typeof(IProjectileGetter) }
    };

    public AutoFillService(IModLogger logger, LinkCacheManager linkCacheManager)
    {
        _logger = logger;
        _linkCacheManager = linkCacheManager;
    }

    /// <summary>
    /// Auto-fill properties for a quest script by parsing its PSC file.
    /// </summary>
    public Result<AutoFillResult> AutoFillQuestScript(
        SkyrimMod mod,
        string questEditorId,
        string scriptName,
        string pscFilePath,
        string dataFolder)
    {
        try
        {
            // Find quest
            var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questEditorId);
            if (quest == null)
            {
                return Result<AutoFillResult>.Fail(
                    $"Quest '{questEditorId}' not found",
                    suggestions: new List<string>
                    {
                        "Use 'esp analyze' to list all quests in the mod",
                        "Ensure the quest was added with 'esp add-quest'"
                    });
            }

            // Get quest adapter
            var adapter = quest.VirtualMachineAdapter as QuestAdapter;
            if (adapter == null)
            {
                return Result<AutoFillResult>.Fail(
                    $"Quest '{questEditorId}' has no scripts attached",
                    suggestions: new List<string>
                    {
                        $"Use 'esp attach-script' to attach '{scriptName}' to the quest first"
                    });
            }

            // Find script
            var script = adapter.Scripts.FirstOrDefault(s => s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
            if (script == null)
            {
                return Result<AutoFillResult>.Fail(
                    $"Script '{scriptName}' not attached to quest '{questEditorId}'",
                    suggestions: new List<string>
                    {
                        $"Use 'esp attach-script' to attach '{scriptName}' to the quest"
                    });
            }

            // Get link cache
            var cacheResult = _linkCacheManager.GetOrCreateLinkCache(dataFolder);
            if (!cacheResult.Success)
            {
                return Result<AutoFillResult>.Fail(cacheResult.Error!, cacheResult.ErrorContext, cacheResult.Suggestions);
            }

            // Parse PSC and auto-fill
            return AutoFillScript(script, pscFilePath, cacheResult.Value!);
        }
        catch (Exception ex)
        {
            return Result<AutoFillResult>.Fail(
                "Failed to auto-fill quest script",
                ex.Message);
        }
    }

    /// <summary>
    /// Auto-fill properties for an alias script.
    /// </summary>
    public Result<AutoFillResult> AutoFillAliasScript(
        SkyrimMod mod,
        string questEditorId,
        string aliasName,
        string scriptName,
        string pscFilePath,
        string dataFolder)
    {
        try
        {
            // Find quest
            var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questEditorId);
            if (quest == null)
            {
                return Result<AutoFillResult>.Fail($"Quest '{questEditorId}' not found");
            }

            // Get quest adapter
            var adapter = quest.VirtualMachineAdapter as QuestAdapter;
            if (adapter == null || adapter.Aliases.Count == 0)
            {
                return Result<AutoFillResult>.Fail(
                    $"Quest '{questEditorId}' has no alias scripts",
                    suggestions: new List<string>
                    {
                        "Use 'esp add-alias' to create an alias with a script"
                    });
            }

            // Find alias fragment
            var fragAlias = adapter.Aliases.FirstOrDefault(a =>
                a.Property.Name.Equals(aliasName, StringComparison.OrdinalIgnoreCase));
            if (fragAlias == null)
            {
                return Result<AutoFillResult>.Fail($"Alias '{aliasName}' not found in quest '{questEditorId}'");
            }

            // Find script in alias
            var script = fragAlias.Scripts.FirstOrDefault(s =>
                s.Name.Equals(scriptName, StringComparison.OrdinalIgnoreCase));
            if (script == null)
            {
                return Result<AutoFillResult>.Fail(
                    $"Script '{scriptName}' not attached to alias '{aliasName}'");
            }

            // Get link cache
            var cacheResult = _linkCacheManager.GetOrCreateLinkCache(dataFolder);
            if (!cacheResult.Success)
            {
                return Result<AutoFillResult>.Fail(cacheResult.Error!, cacheResult.ErrorContext, cacheResult.Suggestions);
            }

            // Parse PSC and auto-fill
            return AutoFillScript(script, pscFilePath, cacheResult.Value!);
        }
        catch (Exception ex)
        {
            return Result<AutoFillResult>.Fail(
                "Failed to auto-fill alias script",
                ex.Message);
        }
    }

    /// <summary>
    /// Auto-fill properties for a script entry.
    /// </summary>
    public Result<AutoFillResult> AutoFillScript(
        ScriptEntry script,
        string pscFilePath,
        ILinkCache linkCache)
    {
        try
        {
            if (!File.Exists(pscFilePath))
            {
                return Result<AutoFillResult>.Fail(
                    $"PSC file not found: {pscFilePath}",
                    suggestions: new List<string>
                    {
                        "Ensure the script source file exists",
                        "Check the --script-dir path is correct"
                    });
            }

            // Extract properties from PSC
            var propertiesResult = ExtractPropertiesWithTypesFromPsc(pscFilePath);
            if (!propertiesResult.Success)
            {
                return Result<AutoFillResult>.Fail(propertiesResult.Error!, propertiesResult.ErrorContext, propertiesResult.Suggestions);
            }

            var properties = propertiesResult.Value!;
            _logger.Debug($"Found {properties.Count} properties in {Path.GetFileName(pscFilePath)}");

            var result = new AutoFillResult
            {
                ScriptName = script.Name,
                TotalProperties = properties.Count
            };

            // Fill each property
            foreach (var prop in properties)
            {
                // Check if property already exists
                if (script.Properties.Any(p => p.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.Debug($"Property '{prop.Name}' already exists, skipping");
                    result.SkippedProperties.Add(prop.Name);
                    continue;
                }

                // Skip primitive types (string, int, float, bool)
                if (string.IsNullOrEmpty(prop.Type) ||
                    prop.Type.Equals("String", StringComparison.OrdinalIgnoreCase) ||
                    prop.Type.Equals("Int", StringComparison.OrdinalIgnoreCase) ||
                    prop.Type.Equals("Float", StringComparison.OrdinalIgnoreCase) ||
                    prop.Type.Equals("Bool", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Debug($"Property '{prop.Name}' is primitive type, skipping");
                    result.SkippedProperties.Add(prop.Name);
                    continue;
                }

                // Get record types for this Papyrus type
                var baseType = prop.Type.Replace("[]", "");  // Remove array marker
                if (!PapyrusTypeToRecordTypes.TryGetValue(baseType, out var recordTypes))
                {
                    _logger.Warning($"Unknown Papyrus type '{baseType}' for property '{prop.Name}'");
                    result.SkippedProperties.Add(prop.Name);
                    continue;
                }

                // Try to resolve FormKey by EditorID
                var formKey = TryFindFormByEditorId(prop.Name, linkCache, recordTypes);
                if (formKey == null)
                {
                    _logger.Debug($"Property '{prop.Name}' not found in Skyrim.esm");
                    result.NotFoundProperties.Add(prop.Name);
                    continue;
                }

                // Add property to script
                if (prop.IsArray)
                {
                    // For array properties, use ScriptObjectListProperty
                    var arrayProp = new ScriptObjectListProperty
                    {
                        Name = prop.Name,
                        Flags = ScriptProperty.Flag.Edited,
                        Objects = new ExtendedList<ScriptObjectProperty>()
                    };

                    // Add the found FormKey as the first element
                    var objProp = new ScriptObjectProperty
                    {
                        Alias = -1,  // -1 means no alias reference
                        Unused = 0,
                        Flags = ScriptProperty.Flag.Edited,
                        Object = formKey.Value.ToNullableLink<ISkyrimMajorRecordGetter>()
                    };
                    arrayProp.Objects.Add(objProp);

                    script.Properties.Add(arrayProp);
                    result.FilledProperties.Add(prop.Name);
                    _logger.Info($"Filled array property '{prop.Name}' with 1 element: {formKey.Value}");
                }
                else
                {
                    // Single object property
                    var objProp = new ScriptObjectProperty
                    {
                        Name = prop.Name,
                        Flags = ScriptProperty.Flag.Edited,
                        Object = formKey.Value.ToNullableLink<ISkyrimMajorRecordGetter>()
                    };

                    script.Properties.Add(objProp);
                    result.FilledProperties.Add(prop.Name);
                    _logger.Info($"Filled property '{prop.Name}' with {formKey.Value}");
                }
            }

            result.FilledCount = result.FilledProperties.Count;
            result.SkippedCount = result.SkippedProperties.Count;
            result.NotFoundCount = result.NotFoundProperties.Count;

            return Result<AutoFillResult>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<AutoFillResult>.Fail(
                "Failed to auto-fill script properties",
                ex.Message);
        }
    }

    /// <summary>
    /// Extract property declarations from a PSC file with type information.
    /// </summary>
    private Result<List<PropertyDefinition>> ExtractPropertiesWithTypesFromPsc(string pscFilePath)
    {
        try
        {
            var content = File.ReadAllText(pscFilePath);
            var properties = new List<PropertyDefinition>();

            // Regex pattern: captures type (with optional []) and property name
            // Example matches:
            //   "Keyword Property LocTypeInn Auto"
            //   "GlobalVariable Property ModEnabled Auto Const"
            //   "Keyword[] Property AllKeywords Auto"
            var pattern = @"^\s*(\w+(?:\[\])?)?\s+Property\s+(\w+)";
            var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            foreach (Match match in regex.Matches(content))
            {
                if (match.Groups.Count >= 3)
                {
                    var typeStr = match.Groups[1].Value;
                    var name = match.Groups[2].Value;

                    var isArray = typeStr.EndsWith("[]");
                    var cleanType = typeStr.Replace("[]", "");

                    properties.Add(new PropertyDefinition
                    {
                        Name = name,
                        Type = cleanType,
                        IsArray = isArray
                    });
                }
            }

            _logger.Debug($"Extracted {properties.Count} properties from PSC");
            return Result<List<PropertyDefinition>>.Ok(properties);
        }
        catch (Exception ex)
        {
            return Result<List<PropertyDefinition>>.Fail(
                "Failed to parse PSC file",
                ex.Message,
                new List<string>
                {
                    "Ensure the PSC file is valid Papyrus syntax",
                    "Check for unusual formatting or encoding issues"
                });
        }
    }

    /// <summary>
    /// Try to find a FormKey by EditorID with type filtering.
    /// </summary>
    private FormKey? TryFindFormByEditorId(
        string editorId,
        ILinkCache linkCache,
        Type[] recordTypes)
    {
        foreach (var recordType in recordTypes)
        {
            try
            {
                // Get all identifiers of this type
                var identifiers = linkCache.AllIdentifiers(recordType);

                foreach (var identifier in identifiers)
                {
                    if (string.Equals(identifier.EditorID, editorId, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Debug($"Found {editorId} as {recordType.Name}: {identifier.FormKey}");
                        return identifier.FormKey;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error searching for {editorId} in {recordType.Name}: {ex.Message}");
            }
        }

        return null;
    }
}

/// <summary>
/// Property definition extracted from PSC.
/// </summary>
public class PropertyDefinition
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsArray { get; set; }
}

/// <summary>
/// Result of auto-fill operation.
/// </summary>
public class AutoFillResult
{
    public string ScriptName { get; set; } = "";
    public int TotalProperties { get; set; }
    public int FilledCount { get; set; }
    public int SkippedCount { get; set; }
    public int NotFoundCount { get; set; }
    public List<string> FilledProperties { get; set; } = new();
    public List<string> SkippedProperties { get; set; } = new();
    public List<string> NotFoundProperties { get; set; } = new();
}
