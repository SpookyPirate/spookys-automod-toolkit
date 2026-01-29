namespace SpookysAutomod.Core.Models;

/// <summary>
/// A difference in a single field between two records
/// </summary>
public class FieldDifference
{
    /// <summary>
    /// The field name
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// The value in the original record
    /// </summary>
    public object? OriginalValue { get; set; }

    /// <summary>
    /// The value in the modified record
    /// </summary>
    public object? ModifiedValue { get; set; }
}
