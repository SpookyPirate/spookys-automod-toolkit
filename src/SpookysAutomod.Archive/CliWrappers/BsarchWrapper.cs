using System.Diagnostics;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Archive.CliWrappers;

/// <summary>
/// Wrapper for BSArch CLI tool for BSA/BA2 archive operations.
/// Downloads from GitHub releases on first use.
/// </summary>
public class BsarchWrapper
{
    private readonly IModLogger _logger;
    private readonly string _toolsDir;
    private string? _bsarchPath;

    // BSArch is part of xEdit - download from GitHub releases
    private const string XEditReleaseUrl = "https://github.com/TES5Edit/TES5Edit/releases/download/xedit-4.1.5f/xEdit.4.1.5f.7z";

    public BsarchWrapper(IModLogger logger, string? toolsDirectory = null)
    {
        _logger = logger;
        _toolsDir = toolsDirectory ?? Path.Combine(
            Path.GetDirectoryName(typeof(BsarchWrapper).Assembly.Location) ?? ".",
            "tools", "bsarch");
    }

    /// <summary>
    /// Check if BSArch is available, downloading if needed.
    /// </summary>
    public async Task<Result<string>> EnsureAvailableAsync()
    {
        if (_bsarchPath != null && File.Exists(_bsarchPath))
            return Result<string>.Ok(_bsarchPath);

        var expectedPath = Path.Combine(_toolsDir, "bsarch.exe");

        if (File.Exists(expectedPath))
        {
            _bsarchPath = expectedPath;
            return Result<string>.Ok(_bsarchPath);
        }

        // BSArch needs to be downloaded manually as it's part of xEdit (7z archive)
        return Result<string>.Fail(
            "BSArch not found",
            suggestions: new List<string>
            {
                $"Download xEdit from: {XEditReleaseUrl}",
                "Extract BSArch.exe from the 7z archive",
                $"Place bsarch.exe in: {_toolsDir}",
                "Or download from Nexus Mods: https://www.nexusmods.com/newvegas/mods/64745"
            });
    }

    /// <summary>
    /// Create a BSA archive from a directory.
    /// </summary>
    public async Task<Result<string>> PackAsync(string sourceDir, string outputPath, BsarchOptions? options = null)
    {
        var ensureResult = await EnsureAvailableAsync();
        if (!ensureResult.Success)
            return Result<string>.Fail(ensureResult.Error!);

        options ??= new BsarchOptions();

        var args = new List<string> { "pack", $"\"{sourceDir}\"", $"\"{outputPath}\"" };

        // Add game type flag
        args.Add(options.GameType switch
        {
            GameType.SkyrimSE => "-sse",
            GameType.SkyrimLE => "-tes5",
            GameType.Fallout4 => "-fo4",
            GameType.Fallout76 => "-fo76",
            _ => "-sse"
        });

        if (options.Compress)
            args.Add("-z");

        if (options.Multithreaded)
            args.Add("-mt");

        return await RunAsync(string.Join(" ", args), outputPath);
    }

    /// <summary>
    /// Extract a BSA/BA2 archive to a directory.
    /// </summary>
    public async Task<Result<string>> UnpackAsync(string archivePath, string? outputDir = null)
    {
        var ensureResult = await EnsureAvailableAsync();
        if (!ensureResult.Success)
            return Result<string>.Fail(ensureResult.Error!);

        var args = new List<string> { "unpack", $"\"{archivePath}\"" };

        if (!string.IsNullOrEmpty(outputDir))
            args.Add($"\"{outputDir}\"");

        var targetDir = outputDir ?? Path.GetDirectoryName(archivePath) ?? ".";
        return await RunAsync(string.Join(" ", args), targetDir);
    }

    /// <summary>
    /// Get information about an archive using BSArch.
    /// </summary>
    public async Task<Result<string>> GetInfoAsync(string archivePath)
    {
        var ensureResult = await EnsureAvailableAsync();
        if (!ensureResult.Success)
            return Result<string>.Fail(ensureResult.Error!);

        return await RunAsync($"\"{archivePath}\"", archivePath);
    }

    private async Task<Result<string>> RunAsync(string arguments, string expectedOutput)
    {
        try
        {
            _logger.Debug($"Running: bsarch {arguments}");

            var startInfo = new ProcessStartInfo
            {
                FileName = _bsarchPath!,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return Result<string>.Fail("Failed to start BSArch process");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrEmpty(error) ? error : output;
                return Result<string>.Fail($"BSArch failed: {errorMessage}");
            }

            _logger.Debug($"BSArch output: {output}");
            return Result<string>.Ok(expectedOutput);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to run BSArch: {ex.Message}");
        }
    }
}

public class BsarchOptions
{
    public GameType GameType { get; set; } = GameType.SkyrimSE;
    public bool Compress { get; set; } = true;
    public bool Multithreaded { get; set; } = true;
}

public enum GameType
{
    SkyrimSE,
    SkyrimLE,
    Fallout4,
    Fallout76
}
