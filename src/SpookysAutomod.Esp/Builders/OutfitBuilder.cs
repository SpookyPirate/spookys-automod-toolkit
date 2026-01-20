using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Outfit records.
/// </summary>
public class OutfitBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Outfit _outfit;

    public OutfitBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _outfit = mod.Outfits.AddNew();
        _outfit.EditorID = editorId;
    }

    /// <summary>
    /// Adds a single item (armor or weapon) to the outfit.
    /// </summary>
    /// <param name="itemFormKey">FormKey of the armor or weapon</param>
    public OutfitBuilder AddItem(FormKey itemFormKey)
    {
        _outfit.Items ??= new ExtendedList<IFormLinkGetter<IOutfitTargetGetter>>();
        _outfit.Items.Add(itemFormKey.ToLink<IOutfitTargetGetter>());
        return this;
    }

    /// <summary>
    /// Adds multiple items to the outfit.
    /// </summary>
    /// <param name="itemFormKeys">Array of FormKeys to add</param>
    public OutfitBuilder AddItems(params FormKey[] itemFormKeys)
    {
        foreach (var itemFormKey in itemFormKeys)
        {
            AddItem(itemFormKey);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple items to the outfit.
    /// </summary>
    /// <param name="itemFormKeys">Collection of FormKeys to add</param>
    public OutfitBuilder AddItems(IEnumerable<FormKey> itemFormKeys)
    {
        foreach (var itemFormKey in itemFormKeys)
        {
            AddItem(itemFormKey);
        }
        return this;
    }

    /// <summary>
    /// Preset: Guard outfit (Iron armor + sword + shield).
    /// </summary>
    public OutfitBuilder AsGuard()
    {
        // Iron armor pieces from Skyrim.esm
        var ironCuirass = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x012E46);
        var ironHelmet = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x012E4D);
        var ironGauntlets = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x012E49);
        var ironBoots = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x012E4B);
        var ironSword = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x012EB7);
        var ironShield = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x012EB6);

        AddItems(ironCuirass, ironHelmet, ironGauntlets, ironBoots, ironSword, ironShield);
        return this;
    }

    /// <summary>
    /// Preset: Farmer outfit (basic clothing).
    /// </summary>
    public OutfitBuilder AsFarmer()
    {
        // Farmer clothes from Skyrim.esm
        var farmerClothes = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x0E7ED);
        var roughspunTunic = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x0E7EE);

        AddItems(farmerClothes, roughspunTunic);
        return this;
    }

    /// <summary>
    /// Preset: Mage outfit (robes + hood).
    /// </summary>
    public OutfitBuilder AsMage()
    {
        // Mage robes from Skyrim.esm
        var mageRobes = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x010FA0);
        var mageHood = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x010FB0);

        AddItems(mageRobes, mageHood);
        return this;
    }

    /// <summary>
    /// Preset: Thief outfit (leather armor).
    /// </summary>
    public OutfitBuilder AsThief()
    {
        // Leather armor pieces from Skyrim.esm
        var leatherArmor = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x03619E);
        var leatherHelmet = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x013920);
        var leatherGauntlets = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x013921);
        var leatherBoots = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x013922);

        AddItems(leatherArmor, leatherHelmet, leatherGauntlets, leatherBoots);
        return this;
    }

    /// <summary>
    /// Builds and returns the Outfit record.
    /// </summary>
    public Outfit Build() => _outfit;
}
