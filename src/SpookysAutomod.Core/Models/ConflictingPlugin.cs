namespace SpookysAutomod.Core.Models;

/// <summary>
/// A single plugin in a conflict report
/// </summary>
public class ConflictingPlugin
{
    /// <summary>
    /// The plugin name
    /// </summary>
    public string PluginName { get; set; } = string.Empty;

    /// <summary>
    /// Load order index (lower = loads earlier)
    /// </summary>
    public int LoadOrder { get; set; }

    /// <summary>
    /// Whether this plugin's version wins
    /// </summary>
    public bool IsWinner { get; set; }
}
