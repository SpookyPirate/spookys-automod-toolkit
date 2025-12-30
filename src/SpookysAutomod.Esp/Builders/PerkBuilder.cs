using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Perk records.
/// </summary>
public class PerkBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Perk _perk;
    private byte _priorityCounter = 0;

    public PerkBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _perk = mod.Perks.AddNew();
        _perk.EditorID = editorId;
    }

    public PerkBuilder WithName(string name)
    {
        _perk.Name = name;
        return this;
    }

    public PerkBuilder WithDescription(string description)
    {
        _perk.Description = description;
        return this;
    }

    public PerkBuilder AsPlayable()
    {
        _perk.Playable = true;
        return this;
    }

    public PerkBuilder AsHidden()
    {
        _perk.Hidden = true;
        return this;
    }

    public PerkBuilder WithLevel(byte level)
    {
        _perk.Level = level;
        return this;
    }

    public PerkBuilder WithNumRanks(byte numRanks)
    {
        _perk.NumRanks = numRanks;
        return this;
    }

    public PerkBuilder WithNextPerk(Mutagen.Bethesda.Plugins.FormKey nextPerkFormKey)
    {
        _perk.NextPerk.SetTo(nextPerkFormKey);
        return this;
    }

    // ============ PERK ENTRIES ============

    /// <summary>
    /// Add a perk entry that modifies a value by multiplying it.
    /// </summary>
    public PerkBuilder WithModifyValue(APerkEntryPointEffect.EntryType entryPoint, float multiplier)
    {
        var entry = new PerkEntryPointModifyValue
        {
            EntryPoint = entryPoint,
            PerkConditionTabCount = 0,
            Priority = _priorityCounter++,
            Modification = PerkEntryPointModifyValue.ModificationType.Multiply,
            Value = multiplier
        };
        _perk.Effects.Add(entry);
        return this;
    }

    /// <summary>
    /// Add a perk entry that adds a flat value.
    /// </summary>
    public PerkBuilder WithAddValue(APerkEntryPointEffect.EntryType entryPoint, float value)
    {
        var entry = new PerkEntryPointModifyValue
        {
            EntryPoint = entryPoint,
            PerkConditionTabCount = 0,
            Priority = _priorityCounter++,
            Modification = PerkEntryPointModifyValue.ModificationType.Add,
            Value = value
        };
        _perk.Effects.Add(entry);
        return this;
    }

    // ============ COMBAT PRESETS ============

    /// <summary>
    /// Increase weapon damage by a percentage.
    /// </summary>
    public PerkBuilder WithWeaponDamageBonus(float percentBonus)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModAttackDamage, 1.0f + percentBonus / 100f);
    }

    /// <summary>
    /// Reduce incoming damage by a percentage.
    /// </summary>
    public PerkBuilder WithDamageReduction(float percentReduction)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModIncomingDamage, 1.0f - percentReduction / 100f);
    }

    /// <summary>
    /// Increase armor rating by a percentage.
    /// </summary>
    public PerkBuilder WithArmorBonus(float percentBonus)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModArmorRating, 1.0f + percentBonus / 100f);
    }

    // ============ MAGIC PRESETS ============

    /// <summary>
    /// Reduce spell cost by a percentage.
    /// </summary>
    public PerkBuilder WithSpellCostReduction(float percentReduction)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModSpellCost, 1.0f - percentReduction / 100f);
    }

    /// <summary>
    /// Increase spell magnitude/damage by a percentage.
    /// </summary>
    public PerkBuilder WithSpellPowerBonus(float percentBonus)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModSpellMagnitude, 1.0f + percentBonus / 100f);
    }

    /// <summary>
    /// Increase spell duration by a percentage.
    /// </summary>
    public PerkBuilder WithSpellDurationBonus(float percentBonus)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModSpellDuration, 1.0f + percentBonus / 100f);
    }

    // ============ STEALTH PRESETS ============

    /// <summary>
    /// Increase sneak attack damage multiplier.
    /// </summary>
    public PerkBuilder WithSneakAttackBonus(float multiplier)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModSneakAttackMult, multiplier);
    }

    /// <summary>
    /// Increase pickpocket success chance.
    /// </summary>
    public PerkBuilder WithPickpocketBonus(float percentBonus)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModPickpocketChance, 1.0f + percentBonus / 100f);
    }

    // ============ TRADING PRESETS ============

    /// <summary>
    /// Improve buying prices (pay less).
    /// </summary>
    public PerkBuilder WithBetterBuyingPrices(float percentReduction)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModBuyPrices, 1.0f - percentReduction / 100f);
    }

    /// <summary>
    /// Improve selling prices (get more).
    /// </summary>
    public PerkBuilder WithBetterSellingPrices(float percentBonus)
    {
        return WithModifyValue(APerkEntryPointEffect.EntryType.ModSellPrices, 1.0f + percentBonus / 100f);
    }

    /// <summary>
    /// Improve both buying and selling prices.
    /// </summary>
    public PerkBuilder WithBetterPrices(float percentBonus)
    {
        WithBetterBuyingPrices(percentBonus);
        WithBetterSellingPrices(percentBonus);
        return this;
    }

    public Perk Build() => _perk;
}
