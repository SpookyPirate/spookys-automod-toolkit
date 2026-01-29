namespace SpookysAutomod.Core.Models;

/// <summary>
/// Report of load order conflicts for a record
/// </summary>
public class ConflictReport
{
    /// <summary>
    /// The FormKey being checked
    /// </summary>
    public string FormKey { get; set; } = string.Empty;

    /// <summary>
    /// The editor ID of the record
    /// </summary>
    public string EditorId { get; set; } = string.Empty;

    /// <summary>
    /// List of plugins that modify this record
    /// </summary>
    public List<ConflictingPlugin> Conflicts { get; set; } = new();

    /// <summary>
    /// The plugin that wins (last in load order)
    /// </summary>
    public string WinningPlugin { get; set; } = string.Empty;
}
