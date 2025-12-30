using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Audio.Services;

/// <summary>
/// High-level service for Skyrim audio file operations.
/// Handles FUZ (voice), XWM (compressed audio), WAV, and LIP (lip sync) files.
/// </summary>
public class AudioService
{
    private readonly IModLogger _logger;

    public AudioService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get information about an audio file.
    /// </summary>
    public Result<AudioInfo> GetInfo(string audioPath)
    {
        if (!File.Exists(audioPath))
        {
            return Result<AudioInfo>.Fail($"File not found: {audioPath}");
        }

        try
        {
            var ext = Path.GetExtension(audioPath).ToLowerInvariant();
            var info = new AudioInfo
            {
                FilePath = audioPath,
                FileName = Path.GetFileName(audioPath),
                FileSize = new FileInfo(audioPath).Length
            };

            switch (ext)
            {
                case ".fuz":
                    return ReadFuzInfo(audioPath, info);
                case ".xwm":
                    return ReadXwmInfo(audioPath, info);
                case ".wav":
                    return ReadWavInfo(audioPath, info);
                case ".lip":
                    info.Type = "LIP (Lip Sync)";
                    return Result<AudioInfo>.Ok(info);
                default:
                    info.Type = "Unknown";
                    return Result<AudioInfo>.Ok(info);
            }
        }
        catch (Exception ex)
        {
            return Result<AudioInfo>.Fail(
                $"Failed to read audio file: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Extract FUZ file to XWM and LIP components.
    /// </summary>
    public Result<FuzExtractResult> ExtractFuz(string fuzPath, string outputDir)
    {
        if (!File.Exists(fuzPath))
        {
            return Result<FuzExtractResult>.Fail($"File not found: {fuzPath}");
        }

        try
        {
            using var stream = File.OpenRead(fuzPath);
            using var reader = new BinaryReader(stream);

            // Read FUZ header
            var magic = reader.ReadUInt32();
            if (magic != 0x5A55465F) // '_FUZ'
            {
                return Result<FuzExtractResult>.Fail("Not a valid FUZ file");
            }

            var version = reader.ReadUInt32();
            var lipSize = reader.ReadUInt32();

            Directory.CreateDirectory(outputDir);
            var baseName = Path.GetFileNameWithoutExtension(fuzPath);

            // Extract LIP
            string? lipPath = null;
            if (lipSize > 0)
            {
                var lipData = reader.ReadBytes((int)lipSize);
                lipPath = Path.Combine(outputDir, $"{baseName}.lip");
                File.WriteAllBytes(lipPath, lipData);
            }

            // Extract XWM (rest of file)
            var xwmSize = stream.Length - stream.Position;
            if (xwmSize > 0)
            {
                var xwmData = reader.ReadBytes((int)xwmSize);
                var xwmPath = Path.Combine(outputDir, $"{baseName}.xwm");
                File.WriteAllBytes(xwmPath, xwmData);

                return Result<FuzExtractResult>.Ok(new FuzExtractResult
                {
                    XwmPath = xwmPath,
                    LipPath = lipPath,
                    OutputDirectory = outputDir
                });
            }

            return Result<FuzExtractResult>.Fail("FUZ file contains no audio data");
        }
        catch (Exception ex)
        {
            return Result<FuzExtractResult>.Fail(
                $"Failed to extract FUZ: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Create a FUZ file from XWM and optional LIP.
    /// </summary>
    public Result<string> CreateFuz(string xwmPath, string? lipPath, string outputPath)
    {
        if (!File.Exists(xwmPath))
        {
            return Result<string>.Fail($"XWM file not found: {xwmPath}");
        }

        if (lipPath != null && !File.Exists(lipPath))
        {
            return Result<string>.Fail($"LIP file not found: {lipPath}");
        }

        try
        {
            var xwmData = File.ReadAllBytes(xwmPath);
            var lipData = lipPath != null ? File.ReadAllBytes(lipPath) : Array.Empty<byte>();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);

            // Write FUZ header
            writer.Write((uint)0x5A55465F); // '_FUZ'
            writer.Write((uint)1);          // Version
            writer.Write((uint)lipData.Length);

            // Write LIP data
            if (lipData.Length > 0)
                writer.Write(lipData);

            // Write XWM data
            writer.Write(xwmData);

            _logger.Info($"Created FUZ: {outputPath}");
            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to create FUZ: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Convert WAV to XWM (requires xWMAEncode.exe).
    /// </summary>
    public Result<string> ConvertWavToXwm(string wavPath, string outputPath)
    {
        if (!File.Exists(wavPath))
        {
            return Result<string>.Fail($"WAV file not found: {wavPath}");
        }

        // xWMAEncode is a Microsoft tool from DirectX SDK
        // For now, return a stub with suggestions
        return Result<string>.Fail(
            "WAV to XWM conversion requires xWMAEncode.exe",
            suggestions: new List<string>
            {
                "Download xWMAEncode from Microsoft DirectX SDK",
                "Use ffmpeg or other audio tools to convert to XWM format",
                "Place xWMAEncode.exe in the tools directory"
            });
    }

    private Result<AudioInfo> ReadFuzInfo(string path, AudioInfo info)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadUInt32();
        if (magic != 0x5A55465F)
        {
            return Result<AudioInfo>.Fail("Not a valid FUZ file");
        }

        var version = reader.ReadUInt32();
        var lipSize = reader.ReadUInt32();

        info.Type = "FUZ (Voice)";
        info.Version = version.ToString();
        info.HasLipSync = lipSize > 0;
        info.LipSyncSize = (int)lipSize;
        info.AudioSize = (int)(stream.Length - 12 - lipSize);

        return Result<AudioInfo>.Ok(info);
    }

    private Result<AudioInfo> ReadXwmInfo(string path, AudioInfo info)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        // XWM is RIFF-based
        var riff = reader.ReadUInt32();
        if (riff != 0x46464952) // 'RIFF'
        {
            info.Type = "XWM (Unknown format)";
            return Result<AudioInfo>.Ok(info);
        }

        reader.ReadUInt32(); // file size
        var wave = reader.ReadUInt32();

        info.Type = wave == 0x45564157 ? "XWM (xWMA Audio)" : "XWM (Unknown)";
        info.AudioSize = (int)info.FileSize;

        return Result<AudioInfo>.Ok(info);
    }

    private Result<AudioInfo> ReadWavInfo(string path, AudioInfo info)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var riff = reader.ReadUInt32();
        if (riff != 0x46464952) // 'RIFF'
        {
            return Result<AudioInfo>.Fail("Not a valid WAV file");
        }

        reader.ReadUInt32(); // file size
        var wave = reader.ReadUInt32();

        if (wave != 0x45564157) // 'WAVE'
        {
            return Result<AudioInfo>.Fail("Not a valid WAV file");
        }

        info.Type = "WAV (PCM Audio)";
        info.AudioSize = (int)info.FileSize;

        // Try to read format chunk
        while (stream.Position < stream.Length - 8)
        {
            var chunkId = reader.ReadUInt32();
            var chunkSize = reader.ReadUInt32();

            if (chunkId == 0x20746D66) // 'fmt '
            {
                var format = reader.ReadUInt16();
                info.Channels = reader.ReadUInt16();
                info.SampleRate = (int)reader.ReadUInt32();
                reader.ReadUInt32(); // byte rate
                reader.ReadUInt16(); // block align
                info.BitsPerSample = reader.ReadUInt16();
                break;
            }

            stream.Position += chunkSize;
        }

        return Result<AudioInfo>.Ok(info);
    }
}

public class AudioInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string Type { get; set; } = "";
    public string? Version { get; set; }
    public bool HasLipSync { get; set; }
    public int LipSyncSize { get; set; }
    public int AudioSize { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitsPerSample { get; set; }
}

public class FuzExtractResult
{
    public string? XwmPath { get; set; }
    public string? LipPath { get; set; }
    public string OutputDirectory { get; set; } = "";
}
