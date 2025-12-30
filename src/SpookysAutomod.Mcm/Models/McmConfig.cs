using System.Text.Json.Serialization;

namespace SpookysAutomod.Mcm.Models;

/// <summary>
/// MCM Helper configuration file model.
/// Based on SkyUI MCM Helper format.
/// </summary>
public class McmConfig
{
    [JsonPropertyName("modName")]
    public string ModName { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("minMcmVersion")]
    public int MinMcmVersion { get; set; } = 2;

    [JsonPropertyName("pluginRequirements")]
    public List<string>? PluginRequirements { get; set; }

    [JsonPropertyName("content")]
    public List<McmPage> Content { get; set; } = new();
}

public class McmPage
{
    [JsonPropertyName("pageDisplayName")]
    public string PageDisplayName { get; set; } = "";

    [JsonPropertyName("content")]
    public List<McmControl> Content { get; set; } = new();
}

public class McmControl
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("help")]
    public string? Help { get; set; }

    // Toggle specific
    [JsonPropertyName("groupControl")]
    public int? GroupControl { get; set; }

    // Slider specific
    [JsonPropertyName("min")]
    public float? Min { get; set; }

    [JsonPropertyName("max")]
    public float? Max { get; set; }

    [JsonPropertyName("step")]
    public float? Step { get; set; }

    [JsonPropertyName("formatString")]
    public string? FormatString { get; set; }

    // Menu/Enum specific
    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("shortNames")]
    public List<string>? ShortNames { get; set; }

    // Input specific
    [JsonPropertyName("valueOptions")]
    public McmValueOptions? ValueOptions { get; set; }

    // Source binding
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    [JsonPropertyName("sourceForm")]
    public string? SourceForm { get; set; }

    [JsonPropertyName("propertyName")]
    public string? PropertyName { get; set; }

    [JsonPropertyName("scriptName")]
    public string? ScriptName { get; set; }

    // Color specific
    [JsonPropertyName("defaultColor")]
    public string? DefaultColor { get; set; }

    // Keymap specific
    [JsonPropertyName("ignoreConflicts")]
    public bool? IgnoreConflicts { get; set; }
}

public class McmValueOptions
{
    [JsonPropertyName("min")]
    public float? Min { get; set; }

    [JsonPropertyName("max")]
    public float? Max { get; set; }

    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    [JsonPropertyName("sourceForm")]
    public string? SourceForm { get; set; }
}

/// <summary>
/// MCM control types.
/// </summary>
public static class McmControlType
{
    public const string Header = "header";
    public const string Text = "text";
    public const string Toggle = "toggle";
    public const string Slider = "slider";
    public const string Menu = "menu";
    public const string Enum = "enum";
    public const string Color = "color";
    public const string Keymap = "keymap";
    public const string Input = "input";
    public const string HiddenToggle = "hiddenToggle";
    public const string Empty = "empty";
}
