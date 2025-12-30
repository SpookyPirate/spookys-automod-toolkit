using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Weapon records.
/// </summary>
public class WeaponBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Weapon _weapon;

    public WeaponBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _weapon = mod.Weapons.AddNew();
        _weapon.EditorID = editorId;
        // Set defaults
        _weapon.BasicStats = new WeaponBasicStats
        {
            Damage = 10,
            Weight = 5,
            Value = 100
        };
    }

    public WeaponBuilder WithName(string name)
    {
        _weapon.Name = name;
        return this;
    }

    public WeaponBuilder WithDescription(string description)
    {
        _weapon.Description = description;
        return this;
    }

    public WeaponBuilder WithDamage(ushort damage)
    {
        _weapon.BasicStats ??= new WeaponBasicStats();
        _weapon.BasicStats.Damage = damage;
        return this;
    }

    public WeaponBuilder WithWeight(float weight)
    {
        _weapon.BasicStats ??= new WeaponBasicStats();
        _weapon.BasicStats.Weight = weight;
        return this;
    }

    public WeaponBuilder WithValue(uint value)
    {
        _weapon.BasicStats ??= new WeaponBasicStats();
        _weapon.BasicStats.Value = value;
        return this;
    }

    public WeaponBuilder WithSpeed(float speed)
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.Speed = speed;
        return this;
    }

    public WeaponBuilder WithReach(float reach)
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.Reach = reach;
        return this;
    }

    public WeaponBuilder AsSword()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.OneHandSword;
        return this;
    }

    public WeaponBuilder AsGreatsword()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.TwoHandSword;
        return this;
    }

    public WeaponBuilder AsDagger()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.OneHandDagger;
        return this;
    }

    public WeaponBuilder AsWarAxe()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.OneHandAxe;
        return this;
    }

    public WeaponBuilder AsBattleaxe()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.TwoHandAxe;
        return this;
    }

    public WeaponBuilder AsMace()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.OneHandMace;
        return this;
    }

    public WeaponBuilder AsWarhammer()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.TwoHandAxe;
        return this;
    }

    public WeaponBuilder AsBow()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.Bow;
        return this;
    }

    public WeaponBuilder AsStaff()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.Staff;
        return this;
    }

    public WeaponBuilder AsCrossbow()
    {
        _weapon.Data ??= new WeaponData();
        _weapon.Data.AnimationType = WeaponAnimationType.Crossbow;
        return this;
    }

    /// <summary>
    /// Sets the weapon's 3D model path.
    /// Use paths relative to Data/Meshes/, e.g., "Weapons\Iron\IronSword.nif"
    /// </summary>
    public WeaponBuilder WithModel(string modelPath)
    {
        _weapon.Model = new Model { File = modelPath };
        return this;
    }

    /// <summary>
    /// Uses a vanilla iron sword model - good for testing.
    /// </summary>
    public WeaponBuilder WithIronSwordModel()
    {
        return WithModel(@"Weapons\Iron\IronSword.nif");
    }

    /// <summary>
    /// Uses a vanilla steel sword model.
    /// </summary>
    public WeaponBuilder WithSteelSwordModel()
    {
        return WithModel(@"Weapons\Steel\SteelSword.nif");
    }

    /// <summary>
    /// Uses a vanilla iron dagger model.
    /// </summary>
    public WeaponBuilder WithIronDaggerModel()
    {
        return WithModel(@"Weapons\Iron\IronDagger.nif");
    }

    /// <summary>
    /// Uses a vanilla hunting bow model.
    /// </summary>
    public WeaponBuilder WithHuntingBowModel()
    {
        return WithModel(@"Weapons\IronBow.nif");
    }

    public Weapon Build() => _weapon;
}
