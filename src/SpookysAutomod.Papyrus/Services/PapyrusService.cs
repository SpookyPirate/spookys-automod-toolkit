using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Papyrus.CliWrappers;

namespace SpookysAutomod.Papyrus.Services;

/// <summary>
/// High-level service for Papyrus script operations.
/// </summary>
public class PapyrusService
{
    private readonly IModLogger _logger;
    private readonly PapyrusCompilerWrapper _compiler;
    private readonly ChampollionWrapper _decompiler;

    public PapyrusService(IModLogger logger, string? toolsDir = null)
    {
        _logger = logger;
        _compiler = new PapyrusCompilerWrapper(logger, toolsDir);
        _decompiler = new ChampollionWrapper(logger, toolsDir);
    }

    /// <summary>
    /// Check if required tools are available.
    /// </summary>
    public ToolStatus GetToolStatus()
    {
        return new ToolStatus
        {
            CompilerAvailable = _compiler.IsAvailable(),
            CompilerPath = _compiler.GetToolPath(),
            DecompilerAvailable = _decompiler.IsAvailable(),
            DecompilerPath = _decompiler.GetToolPath()
        };
    }

    /// <summary>
    /// Download required tools.
    /// </summary>
    public async Task<Result> DownloadToolsAsync()
    {
        var compilerResult = await _compiler.DownloadAsync();
        if (!compilerResult.Success)
        {
            _logger.Warning($"Failed to download compiler: {compilerResult.Error}");
        }

        var decompilerResult = await _decompiler.DownloadAsync();
        if (!decompilerResult.Success)
        {
            _logger.Warning($"Failed to download decompiler: {decompilerResult.Error}");
        }

        if (!compilerResult.Success && !decompilerResult.Success)
        {
            return Result.Fail("Failed to download tools");
        }

        return Result.Ok("Tools downloaded successfully");
    }

    /// <summary>
    /// Compile Papyrus source files.
    /// </summary>
    public async Task<Result<CompileResult>> CompileAsync(
        string source,
        string outputDir,
        string headersDir,
        bool optimize = true)
    {
        return await _compiler.CompileAsync(source, outputDir, headersDir, optimize);
    }

    /// <summary>
    /// Decompile PEX files to source.
    /// </summary>
    public async Task<Result<DecompileResult>> DecompileAsync(
        string pexPath,
        string outputDir)
    {
        if (Directory.Exists(pexPath))
        {
            return await _decompiler.DecompileDirectoryAsync(pexPath, outputDir);
        }
        return await _decompiler.DecompileAsync(pexPath, outputDir);
    }

    /// <summary>
    /// Validate a Papyrus source file (syntax check only).
    /// </summary>
    public Result<ValidationResult> ValidateScript(string pscPath)
    {
        if (!File.Exists(pscPath))
        {
            return Result<ValidationResult>.Fail($"File not found: {pscPath}");
        }

        var content = File.ReadAllText(pscPath);
        var errors = new List<string>();
        var warnings = new List<string>();

        // Basic validation checks
        var lines = content.Split('\n');
        var inFunction = false;
        var functionDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineNum = i + 1;

            // Check for scriptname
            if (i == 0 && !line.StartsWith("Scriptname", StringComparison.OrdinalIgnoreCase))
            {
                if (!line.StartsWith(";"))  // Allow comment at start
                    warnings.Add($"Line {lineNum}: Script should start with 'Scriptname'");
            }

            // Check function balance
            if (line.StartsWith("Function ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Event ", StringComparison.OrdinalIgnoreCase))
            {
                if (inFunction)
                    errors.Add($"Line {lineNum}: Nested function/event definition");
                inFunction = true;
                functionDepth++;
            }

            if (line.StartsWith("EndFunction", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("EndEvent", StringComparison.OrdinalIgnoreCase))
            {
                if (!inFunction)
                    errors.Add($"Line {lineNum}: EndFunction/EndEvent without matching start");
                inFunction = false;
                functionDepth--;
            }

            // Check for common issues
            if (line.Contains(";;"))
                warnings.Add($"Line {lineNum}: Double semicolon (might be typo)");

            if (line.EndsWith("\\") && !line.TrimEnd().EndsWith("\\\\"))
                warnings.Add($"Line {lineNum}: Line continuation not standard in Papyrus");
        }

        if (functionDepth != 0)
        {
            errors.Add($"Unbalanced function/event blocks (depth: {functionDepth})");
        }

        return Result<ValidationResult>.Ok(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        });
    }
}

public class ToolStatus
{
    public bool CompilerAvailable { get; set; }
    public string CompilerPath { get; set; } = "";
    public bool DecompilerAvailable { get; set; }
    public string DecompilerPath { get; set; } = "";
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
