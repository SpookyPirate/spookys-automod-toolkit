using System.Text.Json.Serialization;

namespace SpookysAutomod.Core.Models;

/// <summary>
/// Information about an ESP/ESM/ESL plugin file.
/// </summary>
public class PluginInfo
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Author { get; set; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("isLight")]
    public bool IsLight { get; set; }

    [JsonPropertyName("isMaster")]
    public bool IsMaster { get; set; }

    [JsonPropertyName("masterFiles")]
    public List<string> MasterFiles { get; set; } = new();

    [JsonPropertyName("recordCounts")]
    public Dictionary<string, int> RecordCounts { get; set; } = new();

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }
}
