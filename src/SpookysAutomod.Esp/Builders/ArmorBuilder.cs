using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Armor records.
/// </summary>
public class ArmorBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Armor _armor;

    public ArmorBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _armor = mod.Armors.AddNew();
        _armor.EditorID = editorId;
        _armor.ArmorRating = 10;
        _armor.Value = 100;
        _armor.Weight = 5;
    }

    public ArmorBuilder WithName(string name)
    {
        _armor.Name = name;
        return this;
    }

    public ArmorBuilder WithDescription(string description)
    {
        _armor.Description = description;
        return this;
    }

    public ArmorBuilder WithArmorRating(float rating)
    {
        _armor.ArmorRating = rating;
        return this;
    }

    public ArmorBuilder WithValue(uint value)
    {
        _armor.Value = value;
        return this;
    }

    public ArmorBuilder WithWeight(float weight)
    {
        _armor.Weight = weight;
        return this;
    }

    public ArmorBuilder AsLightArmor()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.ArmorType = ArmorType.LightArmor;
        return this;
    }

    public ArmorBuilder AsHeavyArmor()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.ArmorType = ArmorType.HeavyArmor;
        return this;
    }

    public ArmorBuilder AsClothing()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.ArmorType = ArmorType.Clothing;
        return this;
    }

    public ArmorBuilder ForHead()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.FirstPersonFlags |= BipedObjectFlag.Hair | BipedObjectFlag.LongHair | BipedObjectFlag.Circlet;
        return this;
    }

    public ArmorBuilder ForBody()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.FirstPersonFlags |= BipedObjectFlag.Body;
        return this;
    }

    public ArmorBuilder ForHands()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.FirstPersonFlags |= BipedObjectFlag.Hands;
        return this;
    }

    public ArmorBuilder ForFeet()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.FirstPersonFlags |= BipedObjectFlag.Feet;
        return this;
    }

    public ArmorBuilder ForShield()
    {
        _armor.BodyTemplate ??= new BodyTemplate();
        _armor.BodyTemplate.FirstPersonFlags |= BipedObjectFlag.Shield;
        return this;
    }

    public ArmorBuilder AsPlayable()
    {
        _armor.MajorFlags &= ~Armor.MajorFlag.NonPlayable;
        return this;
    }

    public ArmorBuilder AsNonPlayable()
    {
        _armor.MajorFlags |= Armor.MajorFlag.NonPlayable;
        return this;
    }

    /// <summary>
    /// Sets the armor's world model path (the model shown when dropped).
    /// Use paths relative to Data/Meshes/.
    /// </summary>
    public ArmorBuilder WithWorldModel(string modelPath)
    {
        _armor.WorldModel ??= new GenderedItem<ArmorModel?>(null, null);
        _armor.WorldModel.Male = new ArmorModel { Model = new Model { File = modelPath } };
        _armor.WorldModel.Female = new ArmorModel { Model = new Model { File = modelPath } };
        return this;
    }

    /// <summary>
    /// Sets separate male and female world models.
    /// </summary>
    public ArmorBuilder WithWorldModels(string maleModelPath, string femaleModelPath)
    {
        _armor.WorldModel ??= new GenderedItem<ArmorModel?>(null, null);
        _armor.WorldModel.Male = new ArmorModel { Model = new Model { File = maleModelPath } };
        _armor.WorldModel.Female = new ArmorModel { Model = new Model { File = femaleModelPath } };
        return this;
    }

    /// <summary>
    /// Uses vanilla iron armor cuirass model.
    /// </summary>
    public ArmorBuilder WithIronCuirassModel()
    {
        return WithWorldModels(
            @"Armor\Iron\Male\CuirassGND.nif",
            @"Armor\Iron\Female\CuirassGND.nif");
    }

    /// <summary>
    /// Uses vanilla iron helmet model.
    /// </summary>
    public ArmorBuilder WithIronHelmetModel()
    {
        return WithWorldModel(@"Armor\Iron\Male\HelmetGND.nif");
    }

    /// <summary>
    /// Uses vanilla iron gauntlets model.
    /// </summary>
    public ArmorBuilder WithIronGauntletsModel()
    {
        return WithWorldModel(@"Armor\Iron\Male\GauntletsGND.nif");
    }

    /// <summary>
    /// Uses vanilla iron boots model.
    /// </summary>
    public ArmorBuilder WithIronBootsModel()
    {
        return WithWorldModel(@"Armor\Iron\Male\BootsGND.nif");
    }

    /// <summary>
    /// Uses vanilla iron shield model.
    /// </summary>
    public ArmorBuilder WithIronShieldModel()
    {
        return WithWorldModel(@"Armor\Iron\IronShieldGO.nif");
    }

    public Armor Build() => _armor;
}
