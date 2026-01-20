using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating LeveledItem records.
/// </summary>
public class LeveledItemBuilder
{
    private readonly SkyrimMod _mod;
    private readonly LeveledItem _leveledItem;

    public LeveledItemBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _leveledItem = mod.LeveledItems.AddNew();
        _leveledItem.EditorID = editorId;
        _leveledItem.ChanceNone = new Percent(0);
        _leveledItem.Flags = LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;
        _leveledItem.Entries = new ExtendedList<LeveledItemEntry>();
    }

    /// <summary>
    /// Sets the chance (0-100) that the list returns nothing.
    /// 0 = always returns items, 100 = always empty.
    /// </summary>
    public LeveledItemBuilder WithChanceNone(byte percent)
    {
        if (percent > 100)
        {
            throw new ArgumentException("ChanceNone must be between 0 and 100", nameof(percent));
        }
        _leveledItem.ChanceNone = new Percent(percent / 100.0);
        return this;
    }

    /// <summary>
    /// Sets flag to calculate for each item in count (instead of picking one from list).
    /// </summary>
    public LeveledItemBuilder CalculateForEachItem()
    {
        _leveledItem.Flags |= LeveledItem.Flag.CalculateForEachItemInCount;
        return this;
    }

    /// <summary>
    /// Sets flag to use all items in the list (gives player everything).
    /// </summary>
    public LeveledItemBuilder UseAll()
    {
        _leveledItem.Flags |= LeveledItem.Flag.UseAll;
        return this;
    }

    /// <summary>
    /// Clears the "calculate from all levels" flag, making only exact level matches eligible.
    /// </summary>
    public LeveledItemBuilder ExactLevelOnly()
    {
        _leveledItem.Flags &= ~LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;
        return this;
    }

    /// <summary>
    /// Adds an entry to the leveled list.
    /// </summary>
    /// <param name="itemFormKey">FormKey of the item (weapon, armor, etc.)</param>
    /// <param name="level">Minimum player level for this item to appear</param>
    /// <param name="count">Number of items to give (default 1)</param>
    public LeveledItemBuilder AddEntry(FormKey itemFormKey, short level, short count = 1)
    {
        if (level < 1)
        {
            throw new ArgumentException("Level must be at least 1", nameof(level));
        }

        _leveledItem.Entries ??= new ExtendedList<LeveledItemEntry>();
        _leveledItem.Entries.Add(new LeveledItemEntry
        {
            Data = new LeveledItemEntryData
            {
                Level = level,
                Count = count,
                Reference = itemFormKey.ToLink<IItemGetter>()
            }
        });
        return this;
    }

    /// <summary>
    /// Preset: Low-value treasure (25% chance none, basic items).
    /// Good for common containers, low-level dungeons.
    /// </summary>
    public LeveledItemBuilder AsLowTreasure()
    {
        _leveledItem.ChanceNone = new Percent(0.25);
        _leveledItem.Flags = LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;
        return this;
    }

    /// <summary>
    /// Preset: Medium-value treasure (15% chance none).
    /// Good for mid-level dungeons, boss chests.
    /// </summary>
    public LeveledItemBuilder AsMediumTreasure()
    {
        _leveledItem.ChanceNone = new Percent(0.15);
        _leveledItem.Flags = LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;
        return this;
    }

    /// <summary>
    /// Preset: High-value treasure (5% chance none).
    /// Good for end-game content, unique encounters.
    /// </summary>
    public LeveledItemBuilder AsHighTreasure()
    {
        _leveledItem.ChanceNone = new Percent(0.05);
        _leveledItem.Flags = LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer;
        return this;
    }

    /// <summary>
    /// Preset: Guaranteed loot (0% chance none, gives all items).
    /// Good for quest rewards, guaranteed drops.
    /// </summary>
    public LeveledItemBuilder AsGuaranteedLoot()
    {
        _leveledItem.ChanceNone = new Percent(0);
        _leveledItem.Flags = LeveledItem.Flag.CalculateFromAllLevelsLessThanOrEqualPlayer | LeveledItem.Flag.UseAll;
        return this;
    }

    /// <summary>
    /// Builds and returns the LeveledItem record.
    /// </summary>
    public LeveledItem Build() => _leveledItem;
}
