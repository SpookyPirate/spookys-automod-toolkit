using System.Reflection;
using Mutagen.Bethesda.Skyrim;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Service for inspecting Mutagen record types using reflection.
/// </summary>
public class TypeInspectionService
{
    private readonly IModLogger _logger;

    // Critical notes for specific types
    private static readonly Dictionary<string, List<string>> CriticalNotes = new()
    {
        ["QuestAlias"] = new()
        {
            "VirtualMachineAdapter NOT on QuestAlias!",
            "Use QuestFragmentAlias in quest.VirtualMachineAdapter.Aliases instead"
        },
        ["QuestFragmentAlias"] = new()
        {
            "Property.Object must reference quest FormKey for CK visibility",
            "Located in quest.VirtualMachineAdapter.Aliases, not quest.Aliases"
        },
        ["Quest"] = new()
        {
            "Alias scripts stored in VirtualMachineAdapter.Aliases (QuestFragmentAlias)",
            "Quest scripts stored in VirtualMachineAdapter.Scripts"
        },
        ["QuestAdapter"] = new()
        {
            "Access via quest.VirtualMachineAdapter as QuestAdapter",
            "Contains Scripts[] (quest scripts) and Aliases[] (QuestFragmentAlias)"
        }
    };

    public TypeInspectionService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all Mutagen record types, optionally filtered by pattern.
    /// </summary>
    /// <param name="pattern">Optional pattern to filter types (supports * wildcard)</param>
    /// <returns>List of type information</returns>
    public Result<List<MutagenTypeInfo>> GetAllMutagenTypes(string? pattern = null)
    {
        try
        {
            _logger.Debug($"Inspecting Mutagen types{(pattern != null ? $" matching '{pattern}'" : "")}");

            var assembly = typeof(SkyrimMod).Assembly;
            var types = assembly.GetTypes()
                .Where(t => t.IsPublic && (t.IsClass || t.IsInterface))
                .Where(t => IsRelevantMutagenType(t))
                .ToList();

            // Apply pattern filter if specified
            if (!string.IsNullOrEmpty(pattern))
            {
                var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                types = types.Where(t => regex.IsMatch(t.Name)).ToList();
            }

            var typeInfos = types
                .Select(t => CreateTypeInfo(t))
                .OrderBy(t => t.Name)
                .ToList();

            _logger.Info($"Found {typeInfos.Count} Mutagen type(s)");
            return Result<List<MutagenTypeInfo>>.Ok(typeInfos);
        }
        catch (Exception ex)
        {
            return Result<List<MutagenTypeInfo>>.Fail(
                "Failed to inspect Mutagen types",
                ex.Message,
                new List<string>
                {
                    "Ensure Mutagen.Bethesda.Skyrim assembly is loaded",
                    "Check that pattern syntax is correct (use * for wildcard)"
                });
        }
    }

    /// <summary>
    /// Get detailed information about a specific Mutagen type.
    /// </summary>
    public Result<MutagenTypeInfo> GetTypeInfo(string typeName)
    {
        try
        {
            var assembly = typeof(SkyrimMod).Assembly;
            var type = assembly.GetType($"Mutagen.Bethesda.Skyrim.{typeName}")
                ?? assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);

            if (type == null)
            {
                return Result<MutagenTypeInfo>.Fail(
                    $"Type '{typeName}' not found",
                    suggestions: new List<string>
                    {
                        "Use 'esp debug-types' to list all available types",
                        "Try using a wildcard pattern: 'esp debug-types Quest*'",
                        "Check spelling and capitalization"
                    });
            }

            var typeInfo = CreateTypeInfo(type);
            return Result<MutagenTypeInfo>.Ok(typeInfo);
        }
        catch (Exception ex)
        {
            return Result<MutagenTypeInfo>.Fail(
                $"Failed to inspect type '{typeName}'",
                ex.Message);
        }
    }

    private bool IsRelevantMutagenType(Type type)
    {
        // Include record types, quest-related types, and script-related types
        var name = type.Name;

        // Include getters and setters for major record types
        if (name.EndsWith("Getter") || name.EndsWith("Setter"))
        {
            // Major record types
            if (name.Contains("Quest") || name.Contains("Weapon") || name.Contains("Armor") ||
                name.Contains("Spell") || name.Contains("Perk") || name.Contains("Book") ||
                name.Contains("Npc") || name.Contains("Faction") || name.Contains("Global") ||
                name.Contains("Keyword") || name.Contains("MagicEffect") || name.Contains("Enchantment"))
            {
                return true;
            }
        }

        // Include script-related types
        if (name.Contains("Script") || name.Contains("VirtualMachine") || name.Contains("Property"))
        {
            return true;
        }

        // Include quest-related types (critical for alias work)
        if (name.Contains("Quest") || name.Contains("Alias") || name.Contains("Adapter"))
        {
            return true;
        }

        return false;
    }

    private MutagenTypeInfo CreateTypeInfo(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.Name.Contains("_"))  // Filter internal properties
            .Select(p => new PropertyInfo
            {
                Name = p.Name,
                Type = GetFriendlyTypeName(p.PropertyType),
                IsNullable = IsNullableType(p.PropertyType),
                IsCollection = IsCollectionType(p.PropertyType)
            })
            .OrderBy(p => p.Name)
            .ToList();

        var notes = CriticalNotes.GetValueOrDefault(type.Name, new List<string>());

        return new MutagenTypeInfo
        {
            Name = type.Name,
            FullName = type.FullName ?? type.Name,
            Namespace = type.Namespace ?? "",
            IsInterface = type.IsInterface,
            IsClass = type.IsClass,
            Properties = properties,
            Notes = notes
        };
    }

    private string GetFriendlyTypeName(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GetFriendlyTypeName(underlyingType);
        }

        // Handle generic types (List<T>, IEnumerable<T>, etc.)
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name.Split('`')[0];
            var genericArgs = type.GetGenericArguments();
            var argNames = string.Join(", ", genericArgs.Select(GetFriendlyTypeName));
            return $"{genericTypeName}<{argNames}>";
        }

        // Use short name for common types
        if (type.Namespace?.StartsWith("Mutagen.Bethesda") == true)
        {
            return type.Name;
        }

        if (type.Namespace == "System")
        {
            return type.Name;
        }

        return type.Name;
    }

    private bool IsNullableType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null ||
               (!type.IsValueType && !type.IsGenericParameter);
    }

    private bool IsCollectionType(Type type)
    {
        if (type.IsArray)
            return true;

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(IList<>) ||
                   genericTypeDef == typeof(List<>) ||
                   genericTypeDef == typeof(IEnumerable<>) ||
                   genericTypeDef == typeof(ICollection<>);
        }

        return false;
    }
}

/// <summary>
/// Information about a Mutagen type.
/// </summary>
public class MutagenTypeInfo
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public bool IsInterface { get; set; }
    public bool IsClass { get; set; }
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Information about a property on a Mutagen type.
/// </summary>
public class PropertyInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsNullable { get; set; }
    public bool IsCollection { get; set; }
}
