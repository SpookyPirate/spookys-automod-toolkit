using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Nif.Services;

/// <summary>
/// High-level service for NIF mesh operations.
/// Note: NIF reading/writing requires native dependencies that may not be available.
/// This module provides basic file operations with plans for full mesh editing support.
/// </summary>
public class NifService
{
    private readonly IModLogger _logger;

    public NifService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get basic information about a NIF file by reading its header.
    /// </summary>
    public Result<NifInfo> GetInfo(string nifPath)
    {
        if (!File.Exists(nifPath))
        {
            return Result<NifInfo>.Fail($"File not found: {nifPath}");
        }

        try
        {
            using var stream = File.OpenRead(nifPath);
            using var reader = new BinaryReader(stream);

            // Read NIF header
            var headerLine = ReadString(reader, 64);
            if (!headerLine.StartsWith("Gamebryo") && !headerLine.StartsWith("NetImmerse"))
            {
                return Result<NifInfo>.Fail(
                    "Not a valid NIF file",
                    $"Header: {headerLine.Substring(0, Math.Min(40, headerLine.Length))}");
            }

            var info = new NifInfo
            {
                FilePath = nifPath,
                FileName = Path.GetFileName(nifPath),
                FileSize = new FileInfo(nifPath).Length,
                HeaderString = headerLine.Trim('\0', '\n', '\r')
            };

            // Parse version from header
            if (headerLine.Contains("Version"))
            {
                var verStart = headerLine.IndexOf("Version") + 8;
                var verEnd = headerLine.IndexOf('\n', verStart);
                if (verEnd > verStart)
                {
                    info.Version = headerLine.Substring(verStart, verEnd - verStart).Trim();
                }
            }

            return Result<NifInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return Result<NifInfo>.Fail(
                $"Failed to read NIF: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// List textures referenced in a NIF file by searching for texture path patterns.
    /// </summary>
    public Result<List<string>> ListTextures(string nifPath)
    {
        if (!File.Exists(nifPath))
        {
            return Result<List<string>>.Fail($"File not found: {nifPath}");
        }

        try
        {
            var bytes = File.ReadAllBytes(nifPath);
            var textures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Search for texture paths (look for textures\ or .dds patterns)
            var content = System.Text.Encoding.ASCII.GetString(bytes);

            // Find paths that look like textures
            var patterns = new[] { "textures\\", "textures/", ".dds", ".tga" };
            var words = ExtractStrings(bytes, 8, 256);

            foreach (var word in words)
            {
                var lower = word.ToLowerInvariant();
                if (patterns.Any(p => lower.Contains(p)))
                {
                    // Clean up the path
                    var cleaned = word.Trim('\0', ' ', '\t');
                    if (!string.IsNullOrEmpty(cleaned) && cleaned.Length > 4)
                    {
                        textures.Add(cleaned);
                    }
                }
            }

            return Result<List<string>>.Ok(textures.ToList());
        }
        catch (Exception ex)
        {
            return Result<List<string>>.Fail(
                $"Failed to read textures: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Scale operation is not yet implemented.
    /// </summary>
    public Result<string> Scale(string nifPath, float factor, string outputPath)
    {
        if (!File.Exists(nifPath))
        {
            return Result<string>.Fail($"File not found: {nifPath}");
        }

        return Result<string>.Fail(
            "NIF scaling not yet implemented",
            suggestions: new List<string>
            {
                "Use NifSkope for mesh scaling",
                "Use Outfit Studio for batch operations"
            });
    }

    /// <summary>
    /// Copy a NIF file.
    /// </summary>
    public Result<string> Copy(string nifPath, string outputPath)
    {
        if (!File.Exists(nifPath))
        {
            return Result<string>.Fail($"File not found: {nifPath}");
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(nifPath, outputPath, overwrite: true);
            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to copy NIF: {ex.Message}",
                ex.StackTrace);
        }
    }

    private static string ReadString(BinaryReader reader, int maxLength)
    {
        var chars = new List<char>();
        for (int i = 0; i < maxLength; i++)
        {
            var c = reader.ReadChar();
            chars.Add(c);
            if (c == '\n') break;
        }
        return new string(chars.ToArray());
    }

    private static List<string> ExtractStrings(byte[] data, int minLength, int maxLength)
    {
        var strings = new List<string>();
        var current = new List<byte>();

        foreach (var b in data)
        {
            if (b >= 32 && b < 127) // Printable ASCII
            {
                current.Add(b);
            }
            else
            {
                if (current.Count >= minLength && current.Count <= maxLength)
                {
                    strings.Add(System.Text.Encoding.ASCII.GetString(current.ToArray()));
                }
                current.Clear();
            }
        }

        if (current.Count >= minLength && current.Count <= maxLength)
        {
            strings.Add(System.Text.Encoding.ASCII.GetString(current.ToArray()));
        }

        return strings;
    }
}

public class NifInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string HeaderString { get; set; } = "";
    public string Version { get; set; } = "";
}
