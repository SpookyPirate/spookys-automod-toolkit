namespace SpookysAutomod.Core.Models;

/// <summary>
/// Represents the Skyrim game installation environment.
/// </summary>
public class GameEnvironment
{
    /// <summary>
    /// The target Skyrim release version.
    /// </summary>
    public SkyrimVersion Version { get; set; } = SkyrimVersion.SkyrimSE;

    /// <summary>
    /// Path to the Skyrim Data folder.
    /// </summary>
    public string? DataPath { get; set; }

    /// <summary>
    /// Path to the Papyrus script source headers.
    /// </summary>
    public string? ScriptSourcePath { get; set; }

    /// <summary>
    /// Path to the compiled scripts output.
    /// </summary>
    public string? ScriptOutputPath { get; set; }
}

public enum SkyrimVersion
{
    SkyrimLE,
    SkyrimSE,
    SkyrimAE,
    SkyrimVR,
    SkyrimGOG,
    Enderal,
    EnderalSE
}
