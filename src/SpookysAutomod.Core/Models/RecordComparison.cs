namespace SpookysAutomod.Core.Models;

/// <summary>
/// Comparison between two versions of the same record
/// </summary>
public class RecordComparison
{
    /// <summary>
    /// The original record information
    /// </summary>
    public RecordInfo Original { get; set; } = new();

    /// <summary>
    /// The modified record information
    /// </summary>
    public RecordInfo Modified { get; set; } = new();

    /// <summary>
    /// Differences between the two records
    /// </summary>
    public Dictionary<string, FieldDifference> Differences { get; set; } = new();
}
