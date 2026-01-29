namespace SpookysAutomod.Core.Models;

/// <summary>
/// Result from a batch override operation
/// </summary>
public class BatchOverrideResult
{
    /// <summary>
    /// Number of records successfully modified
    /// </summary>
    public int RecordsModified { get; set; }

    /// <summary>
    /// List of modified record EditorIDs
    /// </summary>
    public List<string> ModifiedRecords { get; set; } = new();

    /// <summary>
    /// Path to the created patch file
    /// </summary>
    public string PatchPath { get; set; } = string.Empty;
}
