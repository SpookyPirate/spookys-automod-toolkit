namespace SpookysAutomod.Core.Models;

/// <summary>
/// Result from searching for records across plugins
/// </summary>
public class RecordSearchResult
{
    /// <summary>
    /// The plugin containing this record
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// The editor ID of the record
    /// </summary>
    public string EditorId { get; set; } = string.Empty;

    /// <summary>
    /// The FormKey (e.g., "MyMod.esp:0x000800")
    /// </summary>
    public string FormKey { get; set; } = string.Empty;

    /// <summary>
    /// The record type (Spell, Weapon, etc.)
    /// </summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the record (if available)
    /// </summary>
    public string? Name { get; set; }
}
