namespace SpookysAutomod.Core.Models;

/// <summary>
/// Detailed information about a plugin record
/// </summary>
public class RecordInfo
{
    /// <summary>
    /// The editor ID of the record
    /// </summary>
    public string EditorId { get; set; } = string.Empty;

    /// <summary>
    /// The FormKey of the record (e.g., "MyMod.esp:0x000800")
    /// </summary>
    public string FormKey { get; set; } = string.Empty;

    /// <summary>
    /// The type of record (Spell, Weapon, Armor, Quest, etc.)
    /// </summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>
    /// Record properties extracted based on record type
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// Conditions attached to this record (if any)
    /// </summary>
    public List<ConditionInfo>? Conditions { get; set; }
}
