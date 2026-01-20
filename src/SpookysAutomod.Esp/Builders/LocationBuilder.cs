using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Location records.
/// </summary>
public class LocationBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Location _location;

    public LocationBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _location = mod.Locations.AddNew();
        _location.EditorID = editorId;
        _location.Keywords = new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
    }

    /// <summary>
    /// Sets the display name for the location.
    /// </summary>
    public LocationBuilder WithName(string name)
    {
        _location.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the parent location (e.g., WhiterunHold for a location in Whiterun).
    /// </summary>
    public LocationBuilder WithParentLocation(FormKey parentLocationFormKey)
    {
        _location.ParentLocation.SetTo(parentLocationFormKey.ToLink<ILocationGetter>());
        return this;
    }

    /// <summary>
    /// Adds a keyword to the location (e.g., LocTypeInn, LocTypeCity).
    /// </summary>
    public LocationBuilder AddKeyword(FormKey keywordFormKey)
    {
        _location.Keywords ??= new ExtendedList<IFormLinkGetter<IKeywordGetter>>();
        _location.Keywords.Add(keywordFormKey.ToLink<IKeywordGetter>());
        return this;
    }

    /// <summary>
    /// Adds multiple keywords to the location.
    /// </summary>
    public LocationBuilder AddKeywords(params FormKey[] keywordFormKeys)
    {
        foreach (var keywordFormKey in keywordFormKeys)
        {
            AddKeyword(keywordFormKey);
        }
        return this;
    }

    /// <summary>
    /// Preset: Inn location (adds LocTypeInn keyword).
    /// Good for taverns, inns, drinking establishments.
    /// </summary>
    public LocationBuilder AsInn()
    {
        // LocTypeInn keyword from Skyrim.esm
        var locTypeInn = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x0008A2A);
        AddKeyword(locTypeInn);
        return this;
    }

    /// <summary>
    /// Preset: City location (adds LocTypeCity keyword).
    /// Good for major settlements, walled cities.
    /// </summary>
    public LocationBuilder AsCity()
    {
        // LocTypeCity keyword from Skyrim.esm
        var locTypeCity = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x13168);
        AddKeyword(locTypeCity);
        return this;
    }

    /// <summary>
    /// Preset: Dungeon location (adds LocTypeDungeon keyword).
    /// Good for caves, ruins, underground areas.
    /// </summary>
    public LocationBuilder AsDungeon()
    {
        // LocTypeDungeon keyword from Skyrim.esm
        var locTypeDungeon = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x120E5);
        AddKeyword(locTypeDungeon);
        return this;
    }

    /// <summary>
    /// Preset: Dwelling location (adds LocTypeDwelling keyword).
    /// Good for player homes, NPC houses.
    /// </summary>
    public LocationBuilder AsDwelling()
    {
        // LocTypeDwelling keyword from Skyrim.esm
        var locTypeDwelling = new FormKey(Mutagen.Bethesda.Skyrim.Constants.Skyrim, 0x00FC05);
        AddKeyword(locTypeDwelling);
        return this;
    }

    /// <summary>
    /// Builds and returns the Location record.
    /// </summary>
    public Location Build() => _location;
}
