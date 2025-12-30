using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Core.Interfaces;

/// <summary>
/// Interface for wrapping external CLI tools.
/// </summary>
public interface ICliWrapper
{
    /// <summary>
    /// Name of the tool (e.g., "papyrus-compiler", "champollion").
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// Check if the tool is available and return version info.
    /// </summary>
    Task<Result<string>> GetVersionAsync();

    /// <summary>
    /// Check if the tool exists in the expected location.
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Get the path where the tool should be located.
    /// </summary>
    string GetToolPath();

    /// <summary>
    /// Download the tool from its source (GitHub releases, etc.).
    /// </summary>
    Task<Result> DownloadAsync();
}
