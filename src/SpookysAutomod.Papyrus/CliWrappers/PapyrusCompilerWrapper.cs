using System.Diagnostics;
using System.Text;
using SpookysAutomod.Core.Interfaces;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Papyrus.CliWrappers;

/// <summary>
/// Wrapper for the Bethesda Papyrus Compiler (PapyrusCompiler.exe).
/// Prefers the Original Compiler over russo-2025 for better compatibility.
/// </summary>
public class PapyrusCompilerWrapper : ICliWrapper
{
    private readonly IModLogger _logger;
    private readonly ToolDownloader _downloader;
    private string? _executablePath;
    private string? _flagsFilePath;

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

        // PREFER Original Bethesda compiler (works better than russo for complex mods)
        var originalCompiler = Path.Combine(toolDir, "papyrus-compiler", "Original Compiler", "PapyrusCompiler.exe");
        if (File.Exists(originalCompiler))
        {
            _executablePath = originalCompiler;
            return originalCompiler;
        }

        // Fallback to russo-2025 compiler
        var russoCompiler = Path.Combine(toolDir, "papyrus-compiler", "papyrus.exe");
        if (File.Exists(russoCompiler))
        {
            _executablePath = russoCompiler;
            return russoCompiler;
        }

        // Last resort: search recursively
        var exeName = OperatingSystem.IsWindows() ? "PapyrusCompiler.exe" : "PapyrusCompiler";
        if (Directory.Exists(toolDir))
        {
            var found = Directory.GetFiles(toolDir, exeName, SearchOption.AllDirectories).FirstOrDefault()
                     ?? Directory.GetFiles(toolDir, "papyrus.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (found != null)
            {
                _executablePath = found;
                return found;
            }
        }

        return originalCompiler; // Return expected path even if not found
    }

    public string GetFlagsFilePath()
    {
        if (_flagsFilePath != null && File.Exists(_flagsFilePath))
            return _flagsFilePath;

        var toolDir = Path.Combine(_downloader.ToolsDirectory, "papyrus-compiler");

        // Look for flags file in Original Compiler directory
        var flagsInOriginal = Path.Combine(toolDir, "papyrus-compiler", "Original Compiler", "TESV_Papyrus_Flags.flg");
        if (File.Exists(flagsInOriginal))
        {
            _flagsFilePath = flagsInOriginal;
            return flagsInOriginal;
        }

        // Look in skyrim-script-headers
        var skyrimHeaders = Path.GetFullPath(Path.Combine(_downloader.ToolsDirectory, "..", "skyrim-script-headers", "TESV_Papyrus_Flags.flg"));
        if (File.Exists(skyrimHeaders))
        {
            _flagsFilePath = skyrimHeaders;
            return skyrimHeaders;
        }

        return flagsInOriginal; // Return expected path
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

        var compilerName = GetToolPath().Contains("PapyrusCompiler.exe")
            ? "Bethesda Papyrus Compiler"
            : "russo-2025 Papyrus Compiler";

        return Result<string>.Ok(compilerName);
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
        string? headersDir = null,
        List<string>? additionalImports = null,
        bool optimize = false)
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

        if (!File.Exists(source) && !Directory.Exists(source))
        {
            return Result<CompileResult>.Fail($"Source not found: {source}");
        }

        Directory.CreateDirectory(outputDir);

        // Build import directories list
        var imports = new List<string>();

        // Add SKSE and SkyUI headers if they exist
        var toolsDir = _downloader.ToolsDirectory;
        var headersRoot = Path.Combine(toolsDir, "papyrus-compiler", "headers");

        if (Directory.Exists(Path.Combine(headersRoot, "skse")))
            imports.Add(Path.Combine(headersRoot, "skse"));
        if (Directory.Exists(Path.Combine(headersRoot, "skyui")))
            imports.Add(Path.Combine(headersRoot, "skyui"));
        if (Directory.Exists(Path.Combine(headersRoot, "skyrim")))
            imports.Add(Path.Combine(headersRoot, "skyrim"));

        // Add fallback skyrim headers
        var skyrimHeaders = Path.GetFullPath(Path.Combine(toolsDir, "..", "skyrim-script-headers"));
        if (Directory.Exists(skyrimHeaders))
            imports.Add(skyrimHeaders);

        // Add user-provided headers
        if (headersDir != null && Directory.Exists(headersDir))
            imports.Add(headersDir);

        if (additionalImports != null)
            imports.AddRange(additionalImports);

        // If compiling a directory, add it to imports so scripts can reference each other
        if (Directory.Exists(source))
            imports.Insert(0, source);

        var compilerPath = GetToolPath();
        var isOriginalCompiler = compilerPath.Contains("PapyrusCompiler.exe");

        // Build arguments based on compiler type
        var args = new StringBuilder();
        args.Append($"\"{source}\"");

        if (isOriginalCompiler)
        {
            // Bethesda Original Compiler syntax
            args.Append($" -output=\"{outputDir}\"");

            if (imports.Count > 0)
            {
                var importPath = string.Join(";", imports);
                args.Append($" -import=\"{importPath}\"");
            }

            // Add flags file
            var flagsFile = GetFlagsFilePath();
            if (File.Exists(flagsFile))
                args.Append($" -flags=\"{flagsFile}\"");

            if (optimize)
                args.Append(" -optimize");
        }
        else
        {
            // russo-2025 compiler syntax
            args.Append($" -o \"{outputDir}\"");

            foreach (var import in imports)
            {
                args.Append($" -h \"{import}\"");
            }

            if (optimize)
                args.Append(" -O");
        }

        _logger.Info($"Compiling: {Path.GetFileName(source)}");
        _logger.Debug($"Compiler: {(isOriginalCompiler ? "Bethesda Original" : "russo-2025")}");
        _logger.Debug($"Import dirs: {string.Join(", ", imports)}");

        var result = await RunAsync(args.ToString(), timeoutMs: 120000);

        if (!result.Success)
        {
            var errorOutput = result.ErrorContext ?? result.Error ?? "Unknown compilation error";
            return Result<CompileResult>.Fail(
                "Compilation failed",
                errorOutput,
                ParseCompilerSuggestions(errorOutput));
        }

        // Parse output
        var output = result.Value ?? "";
        var compiled = CountCompiledScripts(output);
        var errors = ParseErrors(output);
        var warnings = ParseWarnings(output);

        var success = errors.Count == 0;

        return Result<CompileResult>.Ok(new CompileResult
        {
            Success = success,
            CompiledCount = compiled,
            OutputDirectory = outputDir,
            Output = output,
            Errors = errors,
            Warnings = warnings
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

        // Unknown user flag errors
        if (output.Contains("Unknown user flag", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Missing or invalid flags file (TESV_Papyrus_Flags.flg)");
            suggestions.Add("The flags file should be in the compiler directory");
        }

        // Missing script errors
        if (output.Contains("unable to locate script", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Missing import/header files for referenced scripts");
            suggestions.Add("Install required header files:");
            suggestions.Add("  - SKSE: Download from https://skse.silverlock.org/");
            suggestions.Add("  - SkyUI: Download SDK from https://github.com/schlangster/skyui/wiki");
            suggestions.Add("  - Extract to tools/papyrus-compiler/headers/skse and /skyui");
        }

        // SKSE function errors
        if (output.Contains("is not a function or does not exist", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("is undefined", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("GetDisplayName", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("StringUtil", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Script uses SKSE functions - SKSE headers required");
            suggestions.Add("Download SKSE SDK: https://skse.silverlock.org/ (skse64_2_XX_XX_sdk.7z)");
            suggestions.Add("Extract Scripts/Source to tools/papyrus-compiler/headers/skse");
        }

        // SkyUI MCM errors
        if (output.Contains("SKI_ConfigBase", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Script requires SkyUI for MCM support");
            suggestions.Add("Download SkyUI SDK: https://github.com/schlangster/skyui/wiki");
            suggestions.Add("Extract MCM scripts to tools/papyrus-compiler/headers/skyui");
        }

        // Syntax errors
        if (output.Contains("syntax error", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("parse error", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Review the script for syntax errors");
        }

        // Generic missing headers
        if (output.Contains("unknown type", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("invalid type", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Missing script headers - check that required headers are installed");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("Run 'papyrus status' to verify compiler is installed");
            suggestions.Add("Check that all script dependencies are available as headers");
        }

        return suggestions;
    }

    private static int CountCompiledScripts(string output)
    {
        var count = 0;
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("succeeded", StringComparison.OrdinalIgnoreCase) ||
                (line.Contains("Compiling", StringComparison.OrdinalIgnoreCase) &&
                 !line.Contains("failed", StringComparison.OrdinalIgnoreCase)))
            {
                count++;
            }
        }
        return count;
    }

    private static List<string> ParseErrors(string output)
    {
        var errors = new List<string>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            if ((line.Contains(".psc(", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("error", StringComparison.OrdinalIgnoreCase)) &&
                !line.Contains("0 succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !errors.Contains(trimmed))
                {
                    errors.Add(trimmed);
                }
            }
        }

        return errors;
    }

    private static List<string> ParseWarnings(string output)
    {
        var warnings = new List<string>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            if (line.Contains("warning", StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    warnings.Add(trimmed);
                }
            }
        }

        return warnings;
    }
}

public class CompileResult
{
    public bool Success { get; set; }
    public int CompiledCount { get; set; }
    public int TotalCount { get; set; }
    public string OutputDirectory { get; set; } = "";
    public string Output { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
