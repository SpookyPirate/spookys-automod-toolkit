using System.Diagnostics;
using System.Text;
using SpookysAutomod.Core.Interfaces;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Papyrus.CliWrappers;

/// <summary>
/// Wrapper for the Champollion PEX decompiler.
/// </summary>
public class ChampollionWrapper : ICliWrapper
{
    private readonly IModLogger _logger;
    private readonly ToolDownloader _downloader;
    private string? _executablePath;

    public string ToolName => "champollion";

    public ChampollionWrapper(IModLogger logger, string? toolsDir = null)
    {
        _logger = logger;
        _downloader = new ToolDownloader(logger, toolsDir);
    }

    public string GetToolPath()
    {
        if (_executablePath != null && File.Exists(_executablePath))
            return _executablePath;

        var toolDir = Path.Combine(_downloader.ToolsDirectory, "champollion");
        var exeName = OperatingSystem.IsWindows() ? "Champollion.exe" : "Champollion";

        if (Directory.Exists(toolDir))
        {
            // Search for executable (may be in subfolder)
            var found = Directory.GetFiles(toolDir, exeName, SearchOption.AllDirectories).FirstOrDefault()
                     ?? Directory.GetFiles(toolDir, "champollion*", SearchOption.AllDirectories)
                         .FirstOrDefault(f => !f.EndsWith(".dll") && !f.EndsWith(".pdb"));

            if (found != null)
            {
                _executablePath = found;
                return found;
            }
        }

        return Path.Combine(toolDir, exeName);
    }

    public bool IsAvailable()
    {
        var path = GetToolPath();
        return File.Exists(path);
    }

    public async Task<Result<string>> GetVersionAsync()
    {
        if (!IsAvailable())
        {
            return Result<string>.Fail(
                "Champollion not found",
                suggestions: new List<string>
                {
                    "Run 'spookys-automod papyrus download' to download the decompiler"
                });
        }

        // Champollion doesn't have a --version flag, just return "available"
        return Result<string>.Ok("available");
    }

    public async Task<Result> DownloadAsync()
    {
        // Champollion releases are on GitHub (single zip for all platforms)
        var result = await _downloader.DownloadFromGitHubAsync(
            "Orvid",
            "Champollion",
            "Champollion*.zip",  // Universal build
            "champollion");

        return result.Success
            ? Result.Ok($"Downloaded to: {result.Value}")
            : Result.Fail(result.Error!, result.ErrorContext, result.Suggestions);
    }

    /// <summary>
    /// Decompile a PEX file to PSC source.
    /// </summary>
    public async Task<Result<DecompileResult>> DecompileAsync(
        string pexPath,
        string outputDir,
        bool overwrite = true)
    {
        if (!IsAvailable())
        {
            return Result<DecompileResult>.Fail(
                "Champollion not found",
                suggestions: new List<string>
                {
                    "Run 'spookys-automod papyrus download' to download the decompiler"
                });
        }

        if (!File.Exists(pexPath))
        {
            return Result<DecompileResult>.Fail($"PEX file not found: {pexPath}");
        }

        Directory.CreateDirectory(outputDir);

        // Build arguments
        var args = new StringBuilder();
        args.Append($"\"{pexPath}\"");
        args.Append($" --psc \"{outputDir}\"");  // Output directory for PSC files

        _logger.Info($"Decompiling: {pexPath}");
        var result = await RunAsync(args.ToString());

        if (!result.Success)
        {
            var errorOutput = result.ErrorContext ?? result.Error ?? "Unknown decompilation error";
            return Result<DecompileResult>.Fail(
                "Decompilation failed",
                errorOutput,
                ParseDecompilerSuggestions(errorOutput));
        }

        // Find output file
        var scriptName = Path.GetFileNameWithoutExtension(pexPath) + ".psc";
        var outputPath = Path.Combine(outputDir, scriptName);

        return Result<DecompileResult>.Ok(new DecompileResult
        {
            Success = true,
            OutputPath = outputPath,
            Output = result.Value ?? ""
        });
    }

    /// <summary>
    /// Decompile all PEX files in a directory.
    /// </summary>
    public async Task<Result<DecompileResult>> DecompileDirectoryAsync(
        string pexDir,
        string outputDir)
    {
        if (!Directory.Exists(pexDir))
        {
            return Result<DecompileResult>.Fail($"Directory not found: {pexDir}");
        }

        var pexFiles = Directory.GetFiles(pexDir, "*.pex", SearchOption.AllDirectories);

        if (pexFiles.Length == 0)
        {
            return Result<DecompileResult>.Fail(
                "No PEX files found",
                suggestions: new List<string> { "Check the directory contains compiled scripts" });
        }

        Directory.CreateDirectory(outputDir);
        var decompiled = 0;
        var errors = new List<string>();

        foreach (var pex in pexFiles)
        {
            var result = await DecompileAsync(pex, outputDir);
            if (result.Success)
                decompiled++;
            else
                errors.Add($"{Path.GetFileName(pex)}: {result.Error}");
        }

        return Result<DecompileResult>.Ok(new DecompileResult
        {
            Success = errors.Count == 0,
            DecompiledCount = decompiled,
            OutputPath = outputDir,
            Errors = errors
        });
    }

    private async Task<Result<string>> RunAsync(string arguments, int timeoutMs = 60000)
    {
        var path = GetToolPath();

        var psi = new ProcessStartInfo
        {
            FileName = path,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var completed = await Task.Run(() => process.WaitForExit(timeoutMs));

            if (!completed)
            {
                process.Kill();
                return Result<string>.Fail("Process timed out");
            }

            var combined = output.ToString() + error.ToString();

            return process.ExitCode == 0
                ? Result<string>.Ok(combined)
                : Result<string>.Fail("Process failed", combined);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to run decompiler: {ex.Message}");
        }
    }

    private static List<string>? ParseDecompilerSuggestions(string? output)
    {
        if (string.IsNullOrEmpty(output)) return null;

        var suggestions = new List<string>();

        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("unable to locate", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("no such file", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Check that the PEX file exists and path is correct");
        }

        if (output.Contains("corrupted", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("malformed", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("PEX file may be corrupted or from an incompatible game version");
        }

        if (output.Contains("unrecognised option", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("unknown option", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Internal error: Invalid command-line arguments passed to Champollion");
            suggestions.Add("Please report this issue to the toolkit developers");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("Run 'papyrus status' to verify decompiler is installed");
            suggestions.Add("Check that the PEX file is valid and not encrypted/obfuscated");
        }

        return suggestions;
    }
}

public class DecompileResult
{
    public bool Success { get; set; }
    public int DecompiledCount { get; set; }
    public string OutputPath { get; set; } = "";
    public string Output { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}
