using System.Reflection;
using System.Text;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;
using SpookysAutomod.Skse.Models;

namespace SpookysAutomod.Skse.Services;

/// <summary>
/// Service for creating and managing SKSE C++ plugin projects.
/// </summary>
public class SkseProjectService
{
    private readonly IModLogger _logger;
    private readonly Assembly _assembly;

    public SkseProjectService(IModLogger logger)
    {
        _logger = logger;
        _assembly = typeof(SkseProjectService).Assembly;
    }

    /// <summary>
    /// Creates a new SKSE plugin project from a template.
    /// </summary>
    public Result<string> CreateProject(SkseProjectConfig config, string outputPath)
    {
        try
        {
            var templateName = config.Template.ToLowerInvariant() switch
            {
                "basic" => "basic",
                "papyrus-native" => "papyrus-native",
                _ => "basic"
            };

            _logger.Info($"Creating SKSE project '{config.Name}' from template '{templateName}'");

            // Create output directory
            var projectDir = Path.Combine(outputPath, config.Name);
            if (Directory.Exists(projectDir))
            {
                return Result<string>.Fail(
                    $"Directory already exists: {projectDir}",
                    suggestions: new List<string> { "Choose a different project name", "Delete the existing directory first" });
            }

            Directory.CreateDirectory(projectDir);

            // Get all template files
            var templatePrefix = $"SpookysAutomod.Skse.Templates.{templateName.Replace("-", "_")}.";
            var resources = _assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (resources.Count == 0)
            {
                return Result<string>.Fail(
                    $"Template '{templateName}' not found",
                    suggestions: new List<string> { "Available templates: basic, papyrus-native" });
            }

            var replacements = BuildReplacements(config);

            foreach (var resourceName in resources)
            {
                // Resource name format: SpookysAutomod.Skse.Templates.basic.src.main.cpp
                // We need to convert to: src/main.cpp
                var relativePath = resourceName.Substring(templatePrefix.Length);

                // Split by dots and reconstruct the path
                var parts = relativePath.Split('.');
                if (parts.Length >= 2)
                {
                    // Last two parts are filename.extension (e.g., "main" and "cpp")
                    var fileName = parts[^2] + "." + parts[^1];
                    // Everything before that is the directory path
                    var dirParts = parts.Take(parts.Length - 2).ToArray();
                    relativePath = dirParts.Length > 0
                        ? Path.Combine(Path.Combine(dirParts), fileName)
                        : fileName;
                }

                var outputFilePath = Path.Combine(projectDir, relativePath);
                var outputFileDir = Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(outputFileDir))
                {
                    Directory.CreateDirectory(outputFileDir);
                }

                using var stream = _assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();

                // Apply replacements
                foreach (var (placeholder, value) in replacements)
                {
                    content = content.Replace(placeholder, value);
                }

                File.WriteAllText(outputFilePath, content);
                _logger.Debug($"Created: {relativePath}");
            }

            // Write project config
            var configPath = Path.Combine(projectDir, "skse-project.json");
            var configJson = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(configPath, configJson);

            _logger.Info($"SKSE project created at: {projectDir}");

            return Result<string>.Ok(projectDir);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to create SKSE project: {ex.Message}",
                suggestions: new List<string> { "Check write permissions to output directory" });
        }
    }

    /// <summary>
    /// Gets information about an existing SKSE project.
    /// </summary>
    public Result<SkseProjectConfig> GetProjectInfo(string projectPath)
    {
        try
        {
            var configPath = Path.Combine(projectPath, "skse-project.json");
            if (!File.Exists(configPath))
            {
                // Try to infer from CMakeLists.txt
                var cmakePath = Path.Combine(projectPath, "CMakeLists.txt");
                if (!File.Exists(cmakePath))
                {
                    return Result<SkseProjectConfig>.Fail(
                        "Not a valid SKSE project directory",
                        suggestions: new List<string> { "Missing skse-project.json and CMakeLists.txt" });
                }

                var config = InferConfigFromCMake(cmakePath);
                if (config != null)
                {
                    return Result<SkseProjectConfig>.Ok(config);
                }

                return Result<SkseProjectConfig>.Fail(
                    "Could not read project configuration",
                    suggestions: new List<string> { "Ensure skse-project.json exists or CMakeLists.txt is valid" });
            }

            var json = File.ReadAllText(configPath);
            var projectConfig = System.Text.Json.JsonSerializer.Deserialize<SkseProjectConfig>(json);

            if (projectConfig == null)
            {
                return Result<SkseProjectConfig>.Fail("Invalid project configuration");
            }

            return Result<SkseProjectConfig>.Ok(projectConfig);
        }
        catch (Exception ex)
        {
            return Result<SkseProjectConfig>.Fail($"Failed to read project info: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a Papyrus native function to an existing project.
    /// </summary>
    public Result<bool> AddPapyrusFunction(string projectPath, PapyrusNativeFunction function)
    {
        try
        {
            var configResult = GetProjectInfo(projectPath);
            if (!configResult.Success || configResult.Value == null)
            {
                return Result<bool>.Fail("Could not read project configuration");
            }

            var config = configResult.Value;
            if (config.Template != "papyrus-native")
            {
                return Result<bool>.Fail(
                    "Project does not support Papyrus native functions",
                    suggestions: new List<string> { "Create a new project with --template papyrus-native" });
            }

            // Add to config
            config.PapyrusFunctions.Add(function);

            // Update papyrus.h
            var papyrusHeaderPath = Path.Combine(projectPath, "src", "papyrus.h");
            if (File.Exists(papyrusHeaderPath))
            {
                var header = File.ReadAllText(papyrusHeaderPath);
                var declaration = GenerateFunctionDeclaration(function);

                // Insert before closing brace of class
                var insertPoint = header.LastIndexOf("};", StringComparison.Ordinal);
                if (insertPoint > 0)
                {
                    header = header.Insert(insertPoint, $"        {declaration}\n    ");
                    File.WriteAllText(papyrusHeaderPath, header);
                }
            }

            // Update papyrus.cpp
            var papyrusCppPath = Path.Combine(projectPath, "src", "papyrus.cpp");
            if (File.Exists(papyrusCppPath))
            {
                var cpp = File.ReadAllText(papyrusCppPath);

                // Add registration
                var registrationLine = $"        a_vm->RegisterFunction(\"{function.Name}\", \"{config.Name}\", {function.Name});";
                var registerInsertPoint = cpp.IndexOf("SKSE::log::info(\"Registered Papyrus", StringComparison.Ordinal);
                if (registerInsertPoint > 0)
                {
                    cpp = cpp.Insert(registerInsertPoint, registrationLine + "\n\n        ");
                }

                // Add implementation
                var implementation = GenerateFunctionImplementation(config.Name, function);
                cpp += "\n" + implementation;

                File.WriteAllText(papyrusCppPath, cpp);
            }

            // Save updated config
            var configPath = Path.Combine(projectPath, "skse-project.json");
            var configJson = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(configPath, configJson);

            _logger.Info($"Added Papyrus function: {function.Name}");
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to add Papyrus function: {ex.Message}");
        }
    }

    /// <summary>
    /// Lists available SKSE templates.
    /// </summary>
    public Result<IEnumerable<string>> ListTemplates()
    {
        var templates = new[]
        {
            "basic - Simple SKSE plugin with event handlers",
            "papyrus-native - SKSE plugin with Papyrus native function support"
        };

        return Result<IEnumerable<string>>.Ok(templates);
    }

    private Dictionary<string, string> BuildReplacements(SkseProjectConfig config)
    {
        // Parse version string
        var versionParts = config.Version.Split('.');
        var major = versionParts.Length > 0 ? versionParts[0] : "1";
        var minor = versionParts.Length > 1 ? versionParts[1] : "0";
        var patch = versionParts.Length > 2 ? versionParts[2] : "0";

        return new Dictionary<string, string>
        {
            { "{{PROJECT_NAME}}", config.Name },
            { "{{AUTHOR}}", config.Author },
            { "{{DESCRIPTION}}", config.Description },
            { "{{VERSION_MAJOR}}", major },
            { "{{VERSION_MINOR}}", minor },
            { "{{VERSION_PATCH}}", patch }
        };
    }

    private SkseProjectConfig? InferConfigFromCMake(string cmakePath)
    {
        try
        {
            var content = File.ReadAllText(cmakePath);
            var config = new SkseProjectConfig();

            // Try to extract project name
            var projectMatch = System.Text.RegularExpressions.Regex.Match(
                content, @"project\s*\(\s*(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (projectMatch.Success)
            {
                config.Name = projectMatch.Groups[1].Value;
            }

            // Check if it has papyrus support
            if (content.Contains("papyrus", StringComparison.OrdinalIgnoreCase))
            {
                config.Template = "papyrus-native";
            }

            return config;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateFunctionDeclaration(PapyrusNativeFunction function)
    {
        var paramList = new StringBuilder();
        paramList.Append("RE::StaticFunctionTag*");

        foreach (var param in function.Parameters)
        {
            paramList.Append($", {MapPapyrusTypeToCpp(param.Type)} {param.Name}");
        }

        return $"static {MapPapyrusTypeToCpp(function.ReturnType)} {function.Name}({paramList});";
    }

    private string GenerateFunctionImplementation(string projectName, PapyrusNativeFunction function)
    {
        var paramList = new StringBuilder();
        paramList.Append("RE::StaticFunctionTag*");

        foreach (var param in function.Parameters)
        {
            paramList.Append($", {MapPapyrusTypeToCpp(param.Type)} {param.Name}");
        }

        var defaultReturn = function.ReturnType.ToLowerInvariant() switch
        {
            "void" => "",
            "int" => "return 0;",
            "float" => "return 0.0f;",
            "bool" => "return false;",
            "string" => "return \"\";",
            _ => "return nullptr;"
        };

        return $@"    {MapPapyrusTypeToCpp(function.ReturnType)} Papyrus::{function.Name}({paramList})
    {{
        // TODO: Implement {function.Name}
        {defaultReturn}
    }}
";
    }

    private string MapPapyrusTypeToCpp(string papyrusType)
    {
        return papyrusType.ToLowerInvariant() switch
        {
            "int" => "int32_t",
            "float" => "float",
            "bool" => "bool",
            "string" => "RE::BSFixedString",
            "actor" => "RE::Actor*",
            "objectreference" => "RE::TESObjectREFR*",
            "form" => "RE::TESForm*",
            "void" => "void",
            _ => $"RE::{papyrusType}*"
        };
    }
}
