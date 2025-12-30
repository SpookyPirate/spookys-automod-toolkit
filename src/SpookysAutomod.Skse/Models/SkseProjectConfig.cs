namespace SpookysAutomod.Skse.Models;

/// <summary>
/// Configuration for an SKSE plugin project.
/// </summary>
public class SkseProjectConfig
{
    /// <summary>
    /// Plugin name (used for DLL output and namespaces).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Plugin author.
    /// </summary>
    public string Author { get; set; } = "";

    /// <summary>
    /// Plugin description.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Plugin version (e.g., "1.0.0").
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Template type: "basic" or "papyrus-native".
    /// </summary>
    public string Template { get; set; } = "basic";

    /// <summary>
    /// Target Skyrim versions to support.
    /// </summary>
    public List<SkyrimVersion> TargetVersions { get; set; } = new() { SkyrimVersion.SE, SkyrimVersion.AE };

    /// <summary>
    /// Output directory for the generated project.
    /// </summary>
    public string OutputDirectory { get; set; } = "";

    /// <summary>
    /// Papyrus native functions defined for this plugin.
    /// </summary>
    public List<PapyrusNativeFunction> PapyrusFunctions { get; set; } = new();
}

/// <summary>
/// Supported Skyrim versions for SKSE plugins.
/// </summary>
public enum SkyrimVersion
{
    SE,   // Skyrim Special Edition (1.5.x)
    AE,   // Skyrim Anniversary Edition (1.6.x)
    VR,   // Skyrim VR
    GOG   // GOG version
}

/// <summary>
/// Papyrus native function definition.
/// </summary>
public class PapyrusNativeFunction
{
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "void";
    public List<PapyrusParameter> Parameters { get; set; } = new();
    public string ScriptName { get; set; } = "";
    public bool IsLatent { get; set; }
}

/// <summary>
/// Papyrus function parameter.
/// </summary>
public class PapyrusParameter
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
}
