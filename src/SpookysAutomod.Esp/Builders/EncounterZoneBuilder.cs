using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating EncounterZone records.
/// </summary>
public class EncounterZoneBuilder
{
    private readonly SkyrimMod _mod;
    private readonly EncounterZone _encounterZone;

    public EncounterZoneBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _encounterZone = mod.EncounterZones.AddNew();
        _encounterZone.EditorID = editorId;
        _encounterZone.MinLevel = 1;
        _encounterZone.MaxLevel = 0; // 0 = unlimited
    }

    /// <summary>
    /// Sets the minimum level for this encounter zone.
    /// </summary>
    public EncounterZoneBuilder WithMinLevel(byte level)
    {
        _encounterZone.MinLevel = level;
        return this;
    }

    /// <summary>
    /// Sets the maximum level for this encounter zone.
    /// Use 0 for unlimited (scales infinitely with player).
    /// </summary>
    public EncounterZoneBuilder WithMaxLevel(byte level)
    {
        if (level > 0 && level < _encounterZone.MinLevel)
        {
            throw new ArgumentException($"MaxLevel ({level}) cannot be less than MinLevel ({_encounterZone.MinLevel})", nameof(level));
        }

        _encounterZone.MaxLevel = level;
        return this;
    }

    /// <summary>
    /// Sets the zone to never reset (enemies stay defeated).
    /// </summary>
    public EncounterZoneBuilder NeverResets()
    {
        _encounterZone.Flags |= EncounterZone.Flag.NeverResets;
        return this;
    }

    /// <summary>
    /// Disables combat boundary (NPCs can pursue player anywhere).
    /// </summary>
    public EncounterZoneBuilder DisableCombatBoundary()
    {
        _encounterZone.Flags |= EncounterZone.Flag.DisableCombatBoundary;
        return this;
    }

    /// <summary>
    /// Preset: Low-level zone (1-10).
    /// Good for starter areas, tutorial content.
    /// </summary>
    public EncounterZoneBuilder AsLowLevel()
    {
        WithMinLevel(1);
        WithMaxLevel(10);
        return this;
    }

    /// <summary>
    /// Preset: Mid-level zone (10-30).
    /// Good for standard dungeons, mid-game content.
    /// </summary>
    public EncounterZoneBuilder AsMidLevel()
    {
        WithMinLevel(10);
        WithMaxLevel(30);
        return this;
    }

    /// <summary>
    /// Preset: High-level zone (30-50).
    /// Good for end-game content, challenging encounters.
    /// </summary>
    public EncounterZoneBuilder AsHighLevel()
    {
        WithMinLevel(30);
        WithMaxLevel(50);
        return this;
    }

    /// <summary>
    /// Preset: Fully scaling zone (1-unlimited).
    /// Good for quest content that should work at any level.
    /// </summary>
    public EncounterZoneBuilder AsScaling()
    {
        WithMinLevel(1);
        WithMaxLevel(0); // Unlimited
        return this;
    }

    /// <summary>
    /// Builds and returns the EncounterZone record.
    /// </summary>
    public EncounterZone Build() => _encounterZone;
}
