using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating Spell records.
/// </summary>
public class SpellBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Spell _spell;
    private int _effectCounter = 0;

    public SpellBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _spell = mod.Spells.AddNew(editorId);
        _spell.EditorID = editorId;

        // Set sensible defaults
        _spell.Type = SpellType.Spell;
        _spell.CastType = CastType.FireAndForget;
        _spell.TargetType = TargetType.Self;
        _spell.BaseCost = 0;
    }

    /// <summary>
    /// Set the spell display name.
    /// </summary>
    public SpellBuilder WithName(string name)
    {
        _spell.Name = name;
        return this;
    }

    /// <summary>
    /// Set the spell type.
    /// </summary>
    public SpellBuilder WithType(SpellType type)
    {
        _spell.Type = type;
        return this;
    }

    /// <summary>
    /// Set the cast type.
    /// </summary>
    public SpellBuilder WithCastType(CastType castType)
    {
        _spell.CastType = castType;
        return this;
    }

    /// <summary>
    /// Set the target type.
    /// </summary>
    public SpellBuilder WithTargetType(TargetType targetType)
    {
        _spell.TargetType = targetType;
        return this;
    }

    /// <summary>
    /// Set the base magicka cost.
    /// </summary>
    public SpellBuilder WithBaseCost(uint cost)
    {
        _spell.BaseCost = cost;
        return this;
    }

    /// <summary>
    /// Set the cast duration.
    /// </summary>
    public SpellBuilder WithCastDuration(float duration)
    {
        _spell.CastDuration = duration;
        return this;
    }

    /// <summary>
    /// Set the charge time.
    /// </summary>
    public SpellBuilder WithChargeTime(float time)
    {
        _spell.ChargeTime = time;
        return this;
    }

    /// <summary>
    /// Make this a lesser power (no cost, once per day).
    /// </summary>
    public SpellBuilder AsLesserPower()
    {
        _spell.Type = SpellType.LesserPower;
        _spell.BaseCost = 0;
        return this;
    }

    /// <summary>
    /// Make this a greater power (no cost, once per day).
    /// </summary>
    public SpellBuilder AsGreaterPower()
    {
        _spell.Type = SpellType.Power;
        _spell.BaseCost = 0;
        return this;
    }

    /// <summary>
    /// Make this an ability (passive, always active).
    /// </summary>
    public SpellBuilder AsAbility()
    {
        _spell.Type = SpellType.Ability;
        _spell.CastType = CastType.ConstantEffect;
        _spell.TargetType = TargetType.Self;
        _spell.BaseCost = 0;
        return this;
    }

    // ============ MAGIC EFFECTS ============

    /// <summary>
    /// Add a custom magic effect to the spell with specified actor value modifier.
    /// Creates a MagicEffect record and attaches it to the spell.
    /// </summary>
    public SpellBuilder WithValueModEffect(ActorValue actorValue, float magnitude, int duration = 0, int area = 0)
    {
        var effectId = $"{_spell.EditorID}_Effect{++_effectCounter}";
        var magicEffect = _mod.MagicEffects.AddNew(effectId);
        magicEffect.EditorID = effectId;
        magicEffect.Name = $"{actorValue} Effect";

        // Basic configuration - archetype defaults to ValueModifier
        magicEffect.BaseCost = 1;
        magicEffect.Flags = MagicEffect.Flag.NoDeathDispel | MagicEffect.Flag.Recover;
        magicEffect.CastType = _spell.CastType;
        magicEffect.TargetType = _spell.TargetType;
        magicEffect.MinimumSkillLevel = 0;

        // Add the effect to the spell
        var effect = new Effect
        {
            Data = new EffectData
            {
                Magnitude = magnitude,
                Duration = duration,
                Area = area
            }
        };
        effect.BaseEffect.SetTo(magicEffect);
        _spell.Effects.Add(effect);

        return this;
    }

    /// <summary>
    /// Add a damage health effect (destruction spell).
    /// </summary>
    public SpellBuilder WithDamageHealth(float damage, int duration = 0)
    {
        _spell.TargetType = TargetType.Aimed;
        return WithValueModEffect(ActorValue.Health, -damage, duration);
    }

    /// <summary>
    /// Add a restore health effect (restoration spell).
    /// </summary>
    public SpellBuilder WithRestoreHealth(float amount, int duration = 0)
    {
        return WithValueModEffect(ActorValue.Health, amount, duration);
    }

    /// <summary>
    /// Add a damage magicka effect.
    /// </summary>
    public SpellBuilder WithDamageMagicka(float damage, int duration = 0)
    {
        _spell.TargetType = TargetType.Aimed;
        return WithValueModEffect(ActorValue.Magicka, -damage, duration);
    }

    /// <summary>
    /// Add a restore magicka effect.
    /// </summary>
    public SpellBuilder WithRestoreMagicka(float amount, int duration = 0)
    {
        return WithValueModEffect(ActorValue.Magicka, amount, duration);
    }

    /// <summary>
    /// Add a damage stamina effect.
    /// </summary>
    public SpellBuilder WithDamageStamina(float damage, int duration = 0)
    {
        _spell.TargetType = TargetType.Aimed;
        return WithValueModEffect(ActorValue.Stamina, -damage, duration);
    }

    /// <summary>
    /// Add a restore stamina effect.
    /// </summary>
    public SpellBuilder WithRestoreStamina(float amount, int duration = 0)
    {
        return WithValueModEffect(ActorValue.Stamina, amount, duration);
    }

    /// <summary>
    /// Add a fortify health effect (buff max health).
    /// </summary>
    public SpellBuilder WithFortifyHealth(float amount, int duration)
    {
        return WithValueModEffect(ActorValue.Health, amount, duration);
    }

    /// <summary>
    /// Add a fortify magicka effect (buff max magicka).
    /// </summary>
    public SpellBuilder WithFortifyMagicka(float amount, int duration)
    {
        return WithValueModEffect(ActorValue.Magicka, amount, duration);
    }

    /// <summary>
    /// Add a fortify stamina effect (buff max stamina).
    /// </summary>
    public SpellBuilder WithFortifyStamina(float amount, int duration)
    {
        return WithValueModEffect(ActorValue.Stamina, amount, duration);
    }

    /// <summary>
    /// Add a fortify skill effect.
    /// </summary>
    public SpellBuilder WithFortifySkill(ActorValue skill, float amount, int duration)
    {
        return WithValueModEffect(skill, amount, duration);
    }

    /// <summary>
    /// Add a fortify attack damage effect.
    /// </summary>
    public SpellBuilder WithFortifyAttackDamage(float amount, int duration)
    {
        return WithValueModEffect(ActorValue.AttackDamageMult, amount, duration);
    }

    /// <summary>
    /// Add a fortify armor effect.
    /// </summary>
    public SpellBuilder WithFortifyArmor(float amount, int duration)
    {
        return WithValueModEffect(ActorValue.DamageResist, amount, duration);
    }

    /// <summary>
    /// Add a fortify speed/movement effect.
    /// </summary>
    public SpellBuilder WithFortifySpeed(float amount, int duration)
    {
        return WithValueModEffect(ActorValue.SpeedMult, amount, duration);
    }

    // ============ PRESET SPELLS ============

    /// <summary>
    /// Create a basic fire damage spell.
    /// </summary>
    public SpellBuilder AsFireDamageSpell(float damage, uint cost)
    {
        _spell.Type = SpellType.Spell;
        _spell.CastType = CastType.FireAndForget;
        _spell.TargetType = TargetType.Aimed;
        _spell.BaseCost = cost;
        return WithDamageHealth(damage);
    }

    /// <summary>
    /// Create a basic healing spell.
    /// </summary>
    public SpellBuilder AsHealingSpell(float amount, uint cost)
    {
        _spell.Type = SpellType.Spell;
        _spell.CastType = CastType.FireAndForget;
        _spell.TargetType = TargetType.Self;
        _spell.BaseCost = cost;
        return WithRestoreHealth(amount);
    }

    /// <summary>
    /// Create a buff spell that fortifies multiple attributes.
    /// </summary>
    public SpellBuilder AsBuffSpell(int duration, uint cost, float healthBonus = 0, float magickaBonus = 0, float staminaBonus = 0)
    {
        _spell.Type = SpellType.Spell;
        _spell.CastType = CastType.FireAndForget;
        _spell.TargetType = TargetType.Self;
        _spell.BaseCost = cost;

        if (healthBonus > 0) WithFortifyHealth(healthBonus, duration);
        if (magickaBonus > 0) WithFortifyMagicka(magickaBonus, duration);
        if (staminaBonus > 0) WithFortifyStamina(staminaBonus, duration);

        return this;
    }

    /// <summary>
    /// Build and return the spell record.
    /// </summary>
    public Spell Build()
    {
        return _spell;
    }
}
