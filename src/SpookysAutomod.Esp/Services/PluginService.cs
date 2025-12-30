using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Service for creating, loading, and saving ESP/ESM/ESL plugin files.
/// </summary>
public class PluginService
{
    private readonly IModLogger _logger;

    public PluginService(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a new empty plugin.
    /// </summary>
    public Result<string> CreatePlugin(
        string name,
        string outputPath,
        bool isLight = false,
        string? author = null,
        string? description = null)
    {
        try
        {
            // Ensure .esp extension
            if (!name.EndsWith(".esp", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".esm", StringComparison.OrdinalIgnoreCase) &&
                !name.EndsWith(".esl", StringComparison.OrdinalIgnoreCase))
            {
                name += ".esp";
            }

            var modKey = ModKey.FromFileName(name);
            var mod = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);

            // Set header flags for light plugin (ESL flag = 0x200)
            if (isLight)
            {
                mod.ModHeader.Flags |= (SkyrimModHeader.HeaderFlag)0x200;
            }

            if (!string.IsNullOrEmpty(author))
            {
                mod.ModHeader.Author = author;
            }

            if (!string.IsNullOrEmpty(description))
            {
                mod.ModHeader.Description = description;
            }

            // Ensure output directory exists
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var fullPath = Path.IsPathRooted(outputPath)
                ? Path.Combine(outputPath, name)
                : Path.Combine(Directory.GetCurrentDirectory(), outputPath, name);

            mod.WriteToBinary(fullPath);

            _logger.Info($"Created plugin: {fullPath}");
            return Result<string>.Ok(fullPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to create plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the output path is writable",
                    "Check that the plugin name contains only valid characters"
                });
        }
    }

    /// <summary>
    /// Load an existing plugin for reading.
    /// </summary>
    public Result<ISkyrimModGetter> LoadPluginReadOnly(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return Result<ISkyrimModGetter>.Fail(
                    $"Plugin not found: {path}",
                    suggestions: new List<string>
                    {
                        "Check the file path is correct",
                        "Use an absolute path if relative path fails"
                    });
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);
            _logger.Debug($"Loaded plugin (read-only): {path}");
            return Result<ISkyrimModGetter>.Ok(mod);
        }
        catch (Exception ex)
        {
            return Result<ISkyrimModGetter>.Fail(
                $"Failed to load plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the file is a valid Skyrim plugin",
                    "Check if the file is corrupted",
                    "Verify the file is not locked by another process"
                });
        }
    }

    /// <summary>
    /// Load an existing plugin for editing.
    /// </summary>
    public Result<SkyrimMod> LoadPluginForEdit(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return Result<SkyrimMod>.Fail(
                    $"Plugin not found: {path}",
                    suggestions: new List<string>
                    {
                        "Check the file path is correct",
                        "Use an absolute path if relative path fails"
                    });
            }

            // Use import mask to properly initialize FormKey allocation
            var mod = SkyrimMod.CreateFromBinary(
                path,
                SkyrimRelease.SkyrimSE,
                new Mutagen.Bethesda.Plugins.Binary.Parameters.BinaryReadParameters
                {
                    // This ensures the FormKey allocator is properly initialized
                });

            // If the mod is empty or has low FormIDs, set a proper starting point
            // Skyrim mods should allocate FormIDs starting from 0x800
            if (mod.ModHeader.Stats.NextFormID < 0x800)
            {
                mod.ModHeader.Stats.NextFormID = 0x800;
            }

            _logger.Debug($"Loaded plugin (editable): {path}");
            return Result<SkyrimMod>.Ok(mod);
        }
        catch (Exception ex)
        {
            return Result<SkyrimMod>.Fail(
                $"Failed to load plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the file is a valid Skyrim plugin",
                    "Check if the file is corrupted",
                    "Verify the file is not locked by another process"
                });
        }
    }

    /// <summary>
    /// Save a plugin to disk.
    /// </summary>
    public Result SavePlugin(SkyrimMod mod, string? outputPath = null)
    {
        try
        {
            var path = outputPath ?? Path.Combine(Directory.GetCurrentDirectory(), mod.ModKey.FileName);
            mod.WriteToBinary(path);
            _logger.Info($"Saved plugin: {path}");
            return Result.Ok($"Plugin saved to: {path}");
        }
        catch (Exception ex)
        {
            return Result.Fail(
                $"Failed to save plugin: {ex.Message}",
                ex.StackTrace,
                new List<string>
                {
                    "Ensure the output path is writable",
                    "Check that no other process has the file locked"
                });
        }
    }

    /// <summary>
    /// Get information about a plugin.
    /// </summary>
    public Result<PluginInfo> GetPluginInfo(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return Result<PluginInfo>.Fail($"Plugin not found: {path}");
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(path, SkyrimRelease.SkyrimSE);
            var fileInfo = new FileInfo(path);

            var info = new PluginInfo
            {
                FileName = mod.ModKey.FileName,
                FilePath = path,
                Author = mod.ModHeader.Author,
                Description = mod.ModHeader.Description,
                IsLight = mod.ModHeader.Flags.HasFlag((SkyrimModHeader.HeaderFlag)0x200),
                IsMaster = mod.ModKey.Type == ModType.Master,
                FileSize = fileInfo.Length
            };

            // Get master files
            foreach (var master in mod.ModHeader.MasterReferences)
            {
                info.MasterFiles.Add(master.Master.FileName);
            }

            // Count records by type
            info.RecordCounts["Quests"] = mod.Quests.Count;
            info.RecordCounts["Spells"] = mod.Spells.Count;
            info.RecordCounts["Globals"] = mod.Globals.Count;
            info.RecordCounts["NPCs"] = mod.Npcs.Count;
            info.RecordCounts["Weapons"] = mod.Weapons.Count;
            info.RecordCounts["Armors"] = mod.Armors.Count;
            info.RecordCounts["Books"] = mod.Books.Count;
            info.RecordCounts["Perks"] = mod.Perks.Count;
            info.RecordCounts["Factions"] = mod.Factions.Count;
            info.RecordCounts["MiscItems"] = mod.MiscItems.Count;

            info.TotalRecords = info.RecordCounts.Values.Sum();

            return Result<PluginInfo>.Ok(info);
        }
        catch (Exception ex)
        {
            return Result<PluginInfo>.Fail(
                $"Failed to read plugin info: {ex.Message}",
                ex.StackTrace);
        }
    }

    /// <summary>
    /// Generate a SEQ file for quests that start enabled.
    /// </summary>
    public Result<string> GenerateSeqFile(string pluginPath, string outputDir)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                return Result<string>.Fail($"Plugin not found: {pluginPath}");
            }

            var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, SkyrimRelease.SkyrimSE);
            var startEnabledQuests = new List<uint>();

            foreach (var quest in mod.Quests)
            {
                if (quest.Flags.HasFlag(Quest.Flag.StartGameEnabled))
                {
                    startEnabledQuests.Add(quest.FormKey.ID);
                }
            }

            if (startEnabledQuests.Count == 0)
            {
                return Result<string>.Fail(
                    "No start-enabled quests found",
                    suggestions: new List<string>
                    {
                        "Add a quest with StartGameEnabled flag",
                        "SEQ files are only needed for quests that start on game load"
                    });
            }

            // Ensure output directory exists
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var seqFileName = Path.GetFileNameWithoutExtension(mod.ModKey.FileName) + ".seq";
            var seqPath = Path.Combine(outputDir, seqFileName);

            // Write SEQ file (simple format: count + FormIDs)
            using var writer = new BinaryWriter(File.Create(seqPath));
            writer.Write((uint)startEnabledQuests.Count);
            foreach (var formId in startEnabledQuests)
            {
                writer.Write(formId);
            }

            _logger.Info($"Generated SEQ file with {startEnabledQuests.Count} quest(s): {seqPath}");
            return Result<string>.Ok(seqPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Failed to generate SEQ file: {ex.Message}",
                ex.StackTrace);
        }
    }
}
