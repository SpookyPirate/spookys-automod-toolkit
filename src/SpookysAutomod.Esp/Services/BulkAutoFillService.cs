using Mutagen.Bethesda.Skyrim;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Service for batch auto-filling all scripts in a mod.
/// </summary>
public class BulkAutoFillService
{
    private readonly IModLogger _logger;
    private readonly AutoFillService _autoFillService;
    private readonly LinkCacheManager _linkCacheManager;

    public BulkAutoFillService(
        IModLogger logger,
        AutoFillService autoFillService,
        LinkCacheManager linkCacheManager)
    {
        _logger = logger;
        _autoFillService = autoFillService;
        _linkCacheManager = linkCacheManager;
    }

    /// <summary>
    /// Auto-fill all scripts in a mod by scanning for PSC files and processing each script.
    /// </summary>
    /// <param name="mod">The mod to process</param>
    /// <param name="scriptDir">Directory containing PSC source files</param>
    /// <param name="dataFolder">Skyrim Data folder path</param>
    /// <returns>Summary of bulk auto-fill operation</returns>
    public Result<BulkAutoFillResult> AutoFillAll(
        SkyrimMod mod,
        string scriptDir,
        string dataFolder)
    {
        try
        {
            if (!Directory.Exists(scriptDir))
            {
                return Result<BulkAutoFillResult>.Fail(
                    $"Script directory not found: {scriptDir}",
                    suggestions: new List<string>
                    {
                        "Ensure the --script-dir path is correct",
                        "Script source files (.psc) must be available for auto-fill"
                    });
            }

            // Get link cache once for all operations (performance optimization)
            var cacheResult = _linkCacheManager.GetOrCreateLinkCache(dataFolder, useCache: true);
            if (!cacheResult.Success)
            {
                return Result<BulkAutoFillResult>.Fail(
                    cacheResult.Error!,
                    cacheResult.ErrorContext,
                    cacheResult.Suggestions);
            }

            var linkCache = cacheResult.Value!;
            var result = new BulkAutoFillResult();

            _logger.Info($"Starting bulk auto-fill for {mod.ModKey.FileName}");

            // Process all quests
            foreach (var quest in mod.Quests)
            {
                var adapter = quest.VirtualMachineAdapter as QuestAdapter;
                if (adapter == null)
                {
                    continue;
                }

                // Process quest scripts
                foreach (var script in adapter.Scripts)
                {
                    var pscPath = Path.Combine(scriptDir, $"{script.Name}.psc");
                    if (!File.Exists(pscPath))
                    {
                        _logger.Debug($"PSC not found for {script.Name}, skipping");
                        result.SkippedScripts++;
                        result.Details.Add($"Skipped {quest.EditorID}.{script.Name} (no PSC file)");
                        continue;
                    }

                    result.TotalScripts++;

                    var fillResult = _autoFillService.AutoFillScript(script, pscPath, linkCache);
                    if (fillResult.Success)
                    {
                        var fillData = fillResult.Value!;
                        if (fillData.FilledCount > 0)
                        {
                            result.FilledScripts++;
                            result.TotalPropertiesFilled += fillData.FilledCount;
                            result.Details.Add(
                                $"Quest {quest.EditorID}.{script.Name}: " +
                                $"{fillData.FilledCount} filled, {fillData.SkippedCount} skipped, {fillData.NotFoundCount} not found");
                        }
                        else
                        {
                            result.Details.Add($"Quest {quest.EditorID}.{script.Name}: No properties filled");
                        }
                    }
                    else
                    {
                        result.Errors.Add($"Quest {quest.EditorID}.{script.Name}: {fillResult.Error}");
                    }
                }

                // Process alias scripts
                if (adapter.Aliases != null)
                {
                    foreach (var fragAlias in adapter.Aliases)
                    {
                        var aliasName = fragAlias.Property.Name;

                        foreach (var script in fragAlias.Scripts)
                        {
                            var pscPath = Path.Combine(scriptDir, $"{script.Name}.psc");
                            if (!File.Exists(pscPath))
                            {
                                _logger.Debug($"PSC not found for {script.Name}, skipping");
                                result.SkippedScripts++;
                                result.Details.Add($"Skipped {quest.EditorID}.{aliasName}.{script.Name} (no PSC file)");
                                continue;
                            }

                            result.TotalScripts++;

                            var fillResult = _autoFillService.AutoFillScript(script, pscPath, linkCache);
                            if (fillResult.Success)
                            {
                                var fillData = fillResult.Value!;
                                if (fillData.FilledCount > 0)
                                {
                                    result.FilledScripts++;
                                    result.TotalPropertiesFilled += fillData.FilledCount;
                                    result.Details.Add(
                                        $"Alias {quest.EditorID}.{aliasName}.{script.Name}: " +
                                        $"{fillData.FilledCount} filled, {fillData.SkippedCount} skipped, {fillData.NotFoundCount} not found");
                                }
                                else
                                {
                                    result.Details.Add($"Alias {quest.EditorID}.{aliasName}.{script.Name}: No properties filled");
                                }
                            }
                            else
                            {
                                result.Errors.Add($"Alias {quest.EditorID}.{aliasName}.{script.Name}: {fillResult.Error}");
                            }
                        }
                    }
                }
            }

            _logger.Info(
                $"Bulk auto-fill complete: {result.FilledScripts} of {result.TotalScripts} scripts filled, " +
                $"{result.TotalPropertiesFilled} total properties");

            return Result<BulkAutoFillResult>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<BulkAutoFillResult>.Fail(
                "Failed to perform bulk auto-fill",
                ex.Message);
        }
    }

    /// <summary>
    /// Auto-fill all scripts in specific quests only.
    /// </summary>
    public Result<BulkAutoFillResult> AutoFillQuests(
        SkyrimMod mod,
        List<string> questEditorIds,
        string scriptDir,
        string dataFolder)
    {
        try
        {
            // Get link cache
            var cacheResult = _linkCacheManager.GetOrCreateLinkCache(dataFolder, useCache: true);
            if (!cacheResult.Success)
            {
                return Result<BulkAutoFillResult>.Fail(
                    cacheResult.Error!,
                    cacheResult.ErrorContext,
                    cacheResult.Suggestions);
            }

            var linkCache = cacheResult.Value!;
            var result = new BulkAutoFillResult();

            foreach (var questId in questEditorIds)
            {
                var quest = mod.Quests.FirstOrDefault(q => q.EditorID == questId);
                if (quest == null)
                {
                    result.Errors.Add($"Quest '{questId}' not found");
                    continue;
                }

                var adapter = quest.VirtualMachineAdapter as QuestAdapter;
                if (adapter == null)
                {
                    result.Errors.Add($"Quest '{questId}' has no scripts");
                    continue;
                }

                // Process quest scripts
                foreach (var script in adapter.Scripts)
                {
                    var pscPath = Path.Combine(scriptDir, $"{script.Name}.psc");
                    if (!File.Exists(pscPath))
                    {
                        result.SkippedScripts++;
                        continue;
                    }

                    result.TotalScripts++;

                    var fillResult = _autoFillService.AutoFillScript(script, pscPath, linkCache);
                    if (fillResult.Success && fillResult.Value!.FilledCount > 0)
                    {
                        result.FilledScripts++;
                        result.TotalPropertiesFilled += fillResult.Value.FilledCount;
                        result.Details.Add(
                            $"{questId}.{script.Name}: {fillResult.Value.FilledCount} properties filled");
                    }
                }

                // Process alias scripts
                if (adapter.Aliases != null)
                {
                    foreach (var fragAlias in adapter.Aliases)
                    {
                        foreach (var script in fragAlias.Scripts)
                        {
                            var pscPath = Path.Combine(scriptDir, $"{script.Name}.psc");
                            if (!File.Exists(pscPath))
                            {
                                result.SkippedScripts++;
                                continue;
                            }

                            result.TotalScripts++;

                            var fillResult = _autoFillService.AutoFillScript(script, pscPath, linkCache);
                            if (fillResult.Success && fillResult.Value!.FilledCount > 0)
                            {
                                result.FilledScripts++;
                                result.TotalPropertiesFilled += fillResult.Value.FilledCount;
                                result.Details.Add(
                                    $"{questId}.{fragAlias.Property.Name}.{script.Name}: " +
                                    $"{fillResult.Value.FilledCount} properties filled");
                            }
                        }
                    }
                }
            }

            return Result<BulkAutoFillResult>.Ok(result);
        }
        catch (Exception ex)
        {
            return Result<BulkAutoFillResult>.Fail(
                "Failed to auto-fill quests",
                ex.Message);
        }
    }
}

/// <summary>
/// Result of bulk auto-fill operation.
/// </summary>
public class BulkAutoFillResult
{
    public int TotalScripts { get; set; }
    public int FilledScripts { get; set; }
    public int SkippedScripts { get; set; }
    public int TotalPropertiesFilled { get; set; }
    public List<string> Details { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
