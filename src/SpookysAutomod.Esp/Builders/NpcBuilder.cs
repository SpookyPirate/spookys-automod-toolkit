using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Builders;

/// <summary>
/// Fluent builder for creating NPC records.
/// </summary>
public class NpcBuilder
{
    private readonly SkyrimMod _mod;
    private readonly Npc _npc;

    public NpcBuilder(SkyrimMod mod, string editorId)
    {
        _mod = mod;
        _npc = mod.Npcs.AddNew();
        _npc.EditorID = editorId;
        _npc.Configuration = new NpcConfiguration
        {
            Level = new NpcLevel { Level = 1 },
            HealthOffset = 0,
            MagickaOffset = 0,
            StaminaOffset = 0,
            CalcMinLevel = 1,
            CalcMaxLevel = 100
        };
    }

    public NpcBuilder WithName(string name)
    {
        _npc.Name = name;
        return this;
    }

    public NpcBuilder WithShortName(string shortName)
    {
        _npc.ShortName = shortName;
        return this;
    }

    public NpcBuilder WithLevel(short level)
    {
        _npc.Configuration.Level = new NpcLevel { Level = level };
        return this;
    }

    public NpcBuilder WithLevelMultiplier(float multiplier, short min = 1, short max = 100)
    {
        _npc.Configuration.Level = new PcLevelMult { LevelMult = multiplier };
        _npc.Configuration.CalcMinLevel = min;
        _npc.Configuration.CalcMaxLevel = max;
        return this;
    }

    public NpcBuilder WithHealthOffset(short offset)
    {
        _npc.Configuration.HealthOffset = offset;
        return this;
    }

    public NpcBuilder WithMagickaOffset(short offset)
    {
        _npc.Configuration.MagickaOffset = offset;
        return this;
    }

    public NpcBuilder WithStaminaOffset(short offset)
    {
        _npc.Configuration.StaminaOffset = offset;
        return this;
    }

    public NpcBuilder AsFemale()
    {
        _npc.Configuration.Flags |= NpcConfiguration.Flag.Female;
        return this;
    }

    public NpcBuilder AsMale()
    {
        _npc.Configuration.Flags &= ~NpcConfiguration.Flag.Female;
        return this;
    }

    public NpcBuilder AsEssential()
    {
        _npc.Configuration.Flags |= NpcConfiguration.Flag.Essential;
        return this;
    }

    public NpcBuilder AsProtected()
    {
        _npc.Configuration.Flags |= NpcConfiguration.Flag.Protected;
        return this;
    }

    public NpcBuilder AsUnique()
    {
        _npc.Configuration.Flags |= NpcConfiguration.Flag.Unique;
        return this;
    }

    public NpcBuilder AsSummonable()
    {
        _npc.Configuration.Flags |= NpcConfiguration.Flag.Summonable;
        return this;
    }

    public NpcBuilder AsGhost()
    {
        _npc.Configuration.Flags |= NpcConfiguration.Flag.IsGhost;
        return this;
    }

    public NpcBuilder WithRace(FormKey raceFormKey)
    {
        _npc.Race.SetTo(raceFormKey);
        return this;
    }

    public NpcBuilder WithClass(FormKey classFormKey)
    {
        _npc.Class.SetTo(classFormKey);
        return this;
    }

    public NpcBuilder WithVoiceType(FormKey voiceTypeFormKey)
    {
        _npc.Voice.SetTo(voiceTypeFormKey);
        return this;
    }

    public NpcBuilder WithCombatStyle(FormKey combatStyleFormKey)
    {
        _npc.CombatStyle.SetTo(combatStyleFormKey);
        return this;
    }

    public NpcBuilder WithDefaultOutfit(FormKey outfitFormKey)
    {
        _npc.DefaultOutfit.SetTo(outfitFormKey);
        return this;
    }

    public Npc Build() => _npc;
}
