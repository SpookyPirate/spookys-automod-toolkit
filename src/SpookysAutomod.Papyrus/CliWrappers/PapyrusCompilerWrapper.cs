using System.Diagnostics;
using System.Text;
using SpookysAutomod.Core.Interfaces;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Papyrus.CliWrappers;

/// <summary>
/// Wrapper for the russo-2025/papyrus-compiler CLI tool.
/// </summary>
public class PapyrusCompilerWrapper : ICliWrapper
{
    private readonly IModLogger _logger;
    private readonly ToolDownloader _downloader;
    private string? _executablePath;

    public string ToolName => "papyrus-compiler";

    public PapyrusCompilerWrapper(IModLogger logger, string? toolsDir = null)
    {
        _logger = logger;
        _downloader = new ToolDownloader(logger, toolsDir);
    }

    public string GetToolPath()
    {
        if (_executablePath != null && File.Exists(_executablePath))
            return _executablePath;

        var toolDir = Path.Combine(_downloader.ToolsDirectory, "papyrus-compiler");

        // Look for the executable (russo-2025's compiler uses "papyrus.exe")
        var exeName = OperatingSystem.IsWindows() ? "papyrus.exe" : "papyrus";

        if (Directory.Exists(toolDir))
        {
            // Search recursively for the executable
            var found = Directory.GetFiles(toolDir, exeName, SearchOption.AllDirectories).FirstOrDefault();
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
                "papyrus-compiler not found",
                suggestions: new List<string>
                {
                    "Run 'spookys-automod papyrus download' to download the compiler"
                });
        }

        try
        {
            var result = await RunAsync("--version");
            return result.Success
                ? Result<string>.Ok(result.Value?.Trim() ?? "unknown")
                : Result<string>.Fail(result.Error ?? "Failed to get version");
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to get version: {ex.Message}");
        }
    }

    public async Task<Result> DownloadAsync()
    {
        var result = await _downloader.DownloadFromGitHubAsync(
            "russo-2025",
            "papyrus-compiler",
            "papyrus-compiler-windows.zip",  // Windows build (tar.gz for other platforms)
            "papyrus-compiler");

        return result.Success
            ? Result.Ok($"Downloaded to: {result.Value}")
            : Result.Fail(result.Error!, result.ErrorContext, result.Suggestions);
    }

    /// <summary>
    /// Compile a Papyrus source file or directory.
    /// </summary>
    public async Task<Result<CompileResult>> CompileAsync(
        string source,
        string outputDir,
        string headersDir,
        bool optimize = true)
    {
        if (!IsAvailable())
        {
            return Result<CompileResult>.Fail(
                "papyrus-compiler not found",
                suggestions: new List<string>
                {
                    "Run 'spookys-automod papyrus download' to download the compiler"
                });
        }

        // Build arguments
        var args = new StringBuilder();
        args.Append($"\"{source}\"");
        args.Append($" -o \"{outputDir}\"");
        args.Append($" -i \"{headersDir}\"");

        if (optimize)
            args.Append(" -O");

        _logger.Info($"Compiling: {source}");
        var result = await RunAsync(args.ToString());

        if (!result.Success)
        {
            var errorOutput = result.ErrorContext ?? result.Error ?? "Unknown compilation error";
            return Result<CompileResult>.Fail(
                "Compilation failed",
                errorOutput,
                ParseCompilerSuggestions(errorOutput));
        }

        // Parse output to count compiled files
        var output = result.Value ?? "";
        var compiled = output.Split('\n')
            .Count(line => line.Contains("Compiled", StringComparison.OrdinalIgnoreCase));

        return Result<CompileResult>.Ok(new CompileResult
        {
            Success = true,
            CompiledCount = compiled,
            OutputDirectory = outputDir,
            Output = output
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
            return Result<string>.Fail($"Failed to run compiler: {ex.Message}");
        }
    }

    private static List<string>? ParseCompilerSuggestions(string? output)
    {
        if (string.IsNullOrEmpty(output)) return null;

        var suggestions = new List<string>();

        // Check for common error patterns
        if (output.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("unable to locate", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Check that the source file exists and path is correct");
        }

        if (output.Contains("syntax error", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("parse error", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Review the script for syntax errors");
        }

        if (output.Contains("undefined", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("unknown type", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("invalid type", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Missing script headers - install Skyrim script headers (see README 'Papyrus Script Headers' section)");
            suggestions.Add("Ensure headers directory contains Actor.psc, Game.psc, Quest.psc, etc.");
        }

        if (output.Contains("import", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Check that the headers directory path is correct");
        }

        // If no specific suggestions found but compilation failed
        if (suggestions.Count == 0)
        {
            suggestions.Add("Run 'papyrus status' to verify compiler is installed");
            suggestions.Add("Check that headers directory contains Skyrim script headers");
        }

        return suggestions;
    }
}

public class CompileResult
{
    public bool Success { get; set; }
    public int CompiledCount { get; set; }
    public string OutputDirectory { get; set; } = "";
    public string Output { get; set; } = "";
    public List<string> Errors { get; set; } = new();
}
