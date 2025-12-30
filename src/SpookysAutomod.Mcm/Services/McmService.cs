using System.Text.Json;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Mcm.Builders;
using SpookysAutomod.Mcm.Models;

namespace SpookysAutomod.Mcm.Services;

/// <summary>
/// High-level service for MCM Helper configuration operations.
/// </summary>
public class McmService
{
    private readonly IModLogger _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public McmService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a new MCM configuration file.
    /// </summary>
    public Result<string> Create(string modName, string displayName, string outputPath)
    {
        try
        {
            var config = new McmBuilder(modName, displayName)
                .AddPage("General")
                .AddHeader("Settings")
                .AddText("Configure your mod settings below.")
                .AddEmpty()
                .Build();

            var json = JsonSerializer.Serialize(config, _jsonOptions);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, json);

            _logger.Info($"Created MCM config: {outputPath}");
            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to create MCM config: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Load an existing MCM configuration.
    /// </summary>
    public Result<McmConfig> Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return Result<McmConfig>.Fail($"Config not found: {configPath}");
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<McmConfig>(json, _jsonOptions);

            if (config == null)
            {
                return Result<McmConfig>.Fail("Failed to parse MCM config");
            }

            return Result<McmConfig>.Ok(config);
        }
        catch (Exception ex)
        {
            return Result<McmConfig>.Fail(
                $"Failed to load MCM config: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Save an MCM configuration.
    /// </summary>
    public Result<string> Save(McmConfig config, string outputPath)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, json);

            return Result<string>.Ok(outputPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to save MCM config: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Add a toggle to an existing config.
    /// </summary>
    public Result<McmConfig> AddToggle(McmConfig config, string id, string text, string? help = null, string? page = null)
    {
        var targetPage = GetOrCreatePage(config, page);

        targetPage.Content.Add(new McmControl
        {
            Type = McmControlType.Toggle,
            Id = id,
            Text = text,
            Help = help
        });

        return Result<McmConfig>.Ok(config);
    }

    /// <summary>
    /// Add a slider to an existing config.
    /// </summary>
    public Result<McmConfig> AddSlider(McmConfig config, string id, string text, float min, float max, float step = 1, string? help = null, string? page = null)
    {
        var targetPage = GetOrCreatePage(config, page);

        targetPage.Content.Add(new McmControl
        {
            Type = McmControlType.Slider,
            Id = id,
            Text = text,
            Help = help,
            Min = min,
            Max = max,
            Step = step,
            FormatString = "{0}"
        });

        return Result<McmConfig>.Ok(config);
    }

    /// <summary>
    /// Validate an MCM configuration file.
    /// </summary>
    public Result<ValidationResult> Validate(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return Result<ValidationResult>.Fail($"Config not found: {configPath}");
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<McmConfig>(json, _jsonOptions);

            if (config == null)
            {
                errors.Add("Failed to parse JSON");
                return Result<ValidationResult>.Ok(new ValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                });
            }

            // Validate required fields
            if (string.IsNullOrEmpty(config.ModName))
                errors.Add("Missing required field: modName");

            if (string.IsNullOrEmpty(config.DisplayName))
                errors.Add("Missing required field: displayName");

            if (config.Content.Count == 0)
                warnings.Add("Configuration has no pages");

            // Validate controls
            var controlIds = new HashSet<string>();
            foreach (var page in config.Content)
            {
                if (string.IsNullOrEmpty(page.PageDisplayName))
                    warnings.Add("Page has no display name");

                foreach (var control in page.Content)
                {
                    if (!string.IsNullOrEmpty(control.Id))
                    {
                        if (!controlIds.Add(control.Id))
                            errors.Add($"Duplicate control ID: {control.Id}");
                    }

                    // Validate slider ranges
                    if (control.Type == McmControlType.Slider)
                    {
                        if (control.Min == null || control.Max == null)
                            errors.Add($"Slider '{control.Id}' missing min/max values");
                        else if (control.Min >= control.Max)
                            errors.Add($"Slider '{control.Id}' has invalid range (min >= max)");
                    }

                    // Validate menu/enum options
                    if (control.Type == McmControlType.Menu || control.Type == McmControlType.Enum)
                    {
                        if (control.Options == null || control.Options.Count == 0)
                            errors.Add($"Menu/Enum '{control.Id}' has no options");
                    }
                }
            }

            return Result<ValidationResult>.Ok(new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings
            });
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return Result<ValidationResult>.Ok(new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            });
        }
        catch (Exception ex)
        {
            return Result<ValidationResult>.Fail(
                $"Validation failed: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Get MCM config info.
    /// </summary>
    public Result<McmInfo> GetInfo(string configPath)
    {
        var loadResult = Load(configPath);
        if (!loadResult.Success)
            return Result<McmInfo>.Fail(loadResult.Error!);

        var config = loadResult.Value!;
        var controlCount = config.Content.Sum(p => p.Content.Count);

        return Result<McmInfo>.Ok(new McmInfo
        {
            ModName = config.ModName,
            DisplayName = config.DisplayName,
            MinMcmVersion = config.MinMcmVersion,
            PageCount = config.Content.Count,
            ControlCount = controlCount,
            Pages = config.Content.Select(p => new McmPageInfo
            {
                Name = p.PageDisplayName,
                ControlCount = p.Content.Count
            }).ToList()
        });
    }

    private static McmPage GetOrCreatePage(McmConfig config, string? pageName)
    {
        if (pageName != null)
        {
            var existing = config.Content.FirstOrDefault(p =>
                p.PageDisplayName.Equals(pageName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            var newPage = new McmPage { PageDisplayName = pageName };
            config.Content.Add(newPage);
            return newPage;
        }

        if (config.Content.Count == 0)
        {
            var defaultPage = new McmPage { PageDisplayName = "General" };
            config.Content.Add(defaultPage);
            return defaultPage;
        }

        return config.Content[^1];
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class McmInfo
{
    public string ModName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int MinMcmVersion { get; set; }
    public int PageCount { get; set; }
    public int ControlCount { get; set; }
    public List<McmPageInfo> Pages { get; set; } = new();
}

public class McmPageInfo
{
    public string Name { get; set; } = "";
    public int ControlCount { get; set; }
}
