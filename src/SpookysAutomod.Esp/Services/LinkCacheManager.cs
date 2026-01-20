using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Manages creation and caching of Mutagen link caches for performance optimization.
/// </summary>
public class LinkCacheManager
{
    private readonly IModLogger _logger;
    private static ILinkCache? _cachedLinkCache;
    private static DateTime _cacheTimestamp = DateTime.MinValue;
    private static string? _cacheDataFolder;
    private static readonly TimeSpan CacheTimeout = TimeSpan.FromMinutes(5);

    public LinkCacheManager(IModLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get or create a link cache for the Skyrim data folder.
    /// </summary>
    /// <param name="skyrimDataFolder">Path to Skyrim Data folder</param>
    /// <param name="useCache">Whether to use cached link cache</param>
    /// <param name="forceRefresh">Force refresh of cached link cache</param>
    /// <returns>Link cache with Skyrim.esm and DLCs loaded</returns>
    public Result<ILinkCache> GetOrCreateLinkCache(
        string skyrimDataFolder,
        bool useCache = true,
        bool forceRefresh = false)
    {
        try
        {
            // Check if we can use cached link cache
            if (useCache && !forceRefresh && _cachedLinkCache != null)
            {
                var cacheAge = DateTime.Now - _cacheTimestamp;
                if (cacheAge < CacheTimeout && _cacheDataFolder == skyrimDataFolder)
                {
                    _logger.Debug($"Using cached link cache (age: {cacheAge.TotalSeconds:F1}s)");
                    return Result<ILinkCache>.Ok(_cachedLinkCache);
                }
                else
                {
                    _logger.Debug("Cached link cache expired or data folder changed, refreshing");
                }
            }

            // Validate data folder exists
            if (!Directory.Exists(skyrimDataFolder))
            {
                return Result<ILinkCache>.Fail(
                    $"Skyrim Data folder not found: {skyrimDataFolder}",
                    suggestions: new List<string>
                    {
                        "Verify the path to your Skyrim Data folder",
                        "Example: C:/Program Files (x86)/Steam/steamapps/common/Skyrim Special Edition/Data",
                        "Use --data-folder flag to specify the correct path"
                    });
            }

            _logger.Debug($"Creating link cache from: {skyrimDataFolder}");

            // Check for required files
            var skyrimEsmPath = Path.Combine(skyrimDataFolder, "Skyrim.esm");
            if (!File.Exists(skyrimEsmPath))
            {
                return Result<ILinkCache>.Fail(
                    "Skyrim.esm not found in Data folder",
                    $"Expected at: {skyrimEsmPath}",
                    new List<string>
                    {
                        "Ensure you're pointing to the correct Skyrim Data folder",
                        "Skyrim.esm is required for auto-fill to work"
                    });
            }

            // Create load order with Skyrim.esm and common masters
            var loadOrder = new List<IModListingGetter<ISkyrimModGetter>>();

            // Load Skyrim.esm (required)
            var skyrimMod = SkyrimMod.CreateFromBinaryOverlay(skyrimEsmPath, SkyrimRelease.SkyrimSE);
            var skyrimListing = new ModListing<ISkyrimModGetter>(skyrimMod, true);
            loadOrder.Add(skyrimListing);

            // Load Update.esm if available (common)
            var updateEsmPath = Path.Combine(skyrimDataFolder, "Update.esm");
            if (File.Exists(updateEsmPath))
            {
                var updateMod = SkyrimMod.CreateFromBinaryOverlay(updateEsmPath, SkyrimRelease.SkyrimSE);
                var updateListing = new ModListing<ISkyrimModGetter>(updateMod, true);
                loadOrder.Add(updateListing);
                _logger.Debug("Loaded Update.esm");
            }

            // Load DLC masters if available
            var dlcFiles = new[] { "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" };
            foreach (var dlcFile in dlcFiles)
            {
                var dlcPath = Path.Combine(skyrimDataFolder, dlcFile);
                if (File.Exists(dlcPath))
                {
                    var dlcMod = SkyrimMod.CreateFromBinaryOverlay(dlcPath, SkyrimRelease.SkyrimSE);
                    var dlcListing = new ModListing<ISkyrimModGetter>(dlcMod, true);
                    loadOrder.Add(dlcListing);
                    _logger.Debug($"Loaded {dlcFile}");
                }
            }

            // Create link cache from load order
            var linkCache = loadOrder.ToImmutableLinkCache();

            // Cache the result
            _cachedLinkCache = linkCache;
            _cacheTimestamp = DateTime.Now;
            _cacheDataFolder = skyrimDataFolder;

            _logger.Info($"Created link cache with {loadOrder.Count} master file(s)");
            return Result<ILinkCache>.Ok(linkCache);
        }
        catch (Exception ex)
        {
            return Result<ILinkCache>.Fail(
                "Failed to create link cache",
                ex.Message,
                new List<string>
                {
                    "Ensure Skyrim.esm is not corrupted",
                    "Check that no other process has the files locked",
                    "Try running with --no-cache flag"
                });
        }
    }

    /// <summary>
    /// Create a link cache that includes a specific mod and its masters.
    /// </summary>
    public Result<ILinkCache> CreateLinkCacheWithMod(
        string skyrimDataFolder,
        ISkyrimModGetter mod)
    {
        try
        {
            _logger.Debug($"Creating link cache with mod: {mod.ModKey.FileName}");

            // Get base link cache
            var baseCacheResult = GetOrCreateLinkCache(skyrimDataFolder, useCache: true);
            if (!baseCacheResult.Success)
            {
                return baseCacheResult;
            }

            // Create load order with masters + mod
            var loadOrder = new List<IModListingGetter<ISkyrimModGetter>>();

            // Load Skyrim.esm
            var skyrimEsmPath = Path.Combine(skyrimDataFolder, "Skyrim.esm");
            var skyrimMod = SkyrimMod.CreateFromBinaryOverlay(skyrimEsmPath, SkyrimRelease.SkyrimSE);
            loadOrder.Add(new ModListing<ISkyrimModGetter>(skyrimMod, true));

            // Load Update.esm if available
            var updateEsmPath = Path.Combine(skyrimDataFolder, "Update.esm");
            if (File.Exists(updateEsmPath))
            {
                var updateMod = SkyrimMod.CreateFromBinaryOverlay(updateEsmPath, SkyrimRelease.SkyrimSE);
                loadOrder.Add(new ModListing<ISkyrimModGetter>(updateMod, true));
            }

            // Load DLCs
            var dlcFiles = new[] { "Dawnguard.esm", "HearthFires.esm", "Dragonborn.esm" };
            foreach (var dlcFile in dlcFiles)
            {
                var dlcPath = Path.Combine(skyrimDataFolder, dlcFile);
                if (File.Exists(dlcPath))
                {
                    var dlcMod = SkyrimMod.CreateFromBinaryOverlay(dlcPath, SkyrimRelease.SkyrimSE);
                    loadOrder.Add(new ModListing<ISkyrimModGetter>(dlcMod, true));
                }
            }

            // Load mod's masters
            foreach (var master in mod.ModHeader.MasterReferences)
            {
                var masterPath = Path.Combine(skyrimDataFolder, master.Master.FileName);
                if (File.Exists(masterPath))
                {
                    var masterMod = SkyrimMod.CreateFromBinaryOverlay(masterPath, SkyrimRelease.SkyrimSE);
                    loadOrder.Add(new ModListing<ISkyrimModGetter>(masterMod, true));
                    _logger.Debug($"Loaded master: {master.Master.FileName}");
                }
                else
                {
                    _logger.Warning($"Master not found: {master.Master.FileName}");
                }
            }

            // Add the mod itself
            loadOrder.Add(new ModListing<ISkyrimModGetter>(mod, true));

            // Create link cache
            var linkCache = loadOrder.ToImmutableLinkCache();
            _logger.Info($"Created link cache with mod and {loadOrder.Count} total file(s)");

            return Result<ILinkCache>.Ok(linkCache);
        }
        catch (Exception ex)
        {
            return Result<ILinkCache>.Fail(
                "Failed to create link cache with mod",
                ex.Message);
        }
    }

    /// <summary>
    /// Clear the cached link cache, forcing a refresh on next access.
    /// </summary>
    public void ClearCache()
    {
        _logger.Debug("Clearing cached link cache");
        _cachedLinkCache = null;
        _cacheTimestamp = DateTime.MinValue;
        _cacheDataFolder = null;
    }

    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public CacheStats GetCacheStats()
    {
        var cacheAge = _cachedLinkCache != null
            ? DateTime.Now - _cacheTimestamp
            : TimeSpan.Zero;

        return new CacheStats
        {
            IsCached = _cachedLinkCache != null,
            CacheAge = cacheAge,
            CacheDataFolder = _cacheDataFolder,
            IsExpired = cacheAge >= CacheTimeout
        };
    }
}

/// <summary>
/// Statistics about the link cache.
/// </summary>
public class CacheStats
{
    public bool IsCached { get; set; }
    public TimeSpan CacheAge { get; set; }
    public string? CacheDataFolder { get; set; }
    public bool IsExpired { get; set; }
}
