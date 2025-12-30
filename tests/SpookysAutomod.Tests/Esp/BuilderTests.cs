using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using SpookysAutomod.Esp.Builders;

namespace SpookysAutomod.Tests.Esp;

public class BuilderTests
{
    private SkyrimMod CreateTestMod() =>
        new SkyrimMod(ModKey.FromFileName("TestMod.esp"), SkyrimRelease.SkyrimSE);

    #region Quest Builder Tests

    [Fact]
    public void QuestBuilder_Build_CreatesQuest()
    {
        var mod = CreateTestMod();
        var quest = new QuestBuilder(mod, "TestQuest")
            .WithName("Test Quest Name")
            .Build();

        Assert.NotNull(quest);
        Assert.Equal("TestQuest", quest.EditorID);
        Assert.Equal("Test Quest Name", quest.Name?.String);
    }

    [Fact]
    public void QuestBuilder_StartEnabled_SetsFlag()
    {
        var mod = CreateTestMod();
        var quest = new QuestBuilder(mod, "EnabledQuest")
            .StartEnabled()
            .Build();

        Assert.True(quest.Flags.HasFlag(Quest.Flag.StartGameEnabled));
    }

    [Fact]
    public void QuestBuilder_RunOnce_SetsFlag()
    {
        var mod = CreateTestMod();
        var quest = new QuestBuilder(mod, "RunOnceQuest")
            .RunOnce()
            .Build();

        Assert.True(quest.Flags.HasFlag(Quest.Flag.RunOnce));
    }

    #endregion

    #region Spell Builder Tests

    [Fact]
    public void SpellBuilder_Build_CreatesSpell()
    {
        var mod = CreateTestMod();
        var spell = new SpellBuilder(mod, "TestSpell")
            .WithName("Test Spell")
            .Build();

        Assert.NotNull(spell);
        Assert.Equal("TestSpell", spell.EditorID);
        Assert.Equal("Test Spell", spell.Name?.String);
    }

    [Fact]
    public void SpellBuilder_WithDamageHealth_CreatesEffect()
    {
        var mod = CreateTestMod();
        var spell = new SpellBuilder(mod, "DamageSpell")
            .WithDamageHealth(50, 0)
            .Build();

        Assert.NotNull(spell);
        Assert.NotEmpty(spell.Effects);
    }

    [Fact]
    public void SpellBuilder_WithBaseCost_SetsCost()
    {
        var mod = CreateTestMod();
        var spell = new SpellBuilder(mod, "CostlySpell")
            .WithBaseCost(100)
            .Build();

        Assert.Equal(100u, spell.BaseCost);
    }

    [Fact]
    public void SpellBuilder_AsAbility_SetsType()
    {
        var mod = CreateTestMod();
        var spell = new SpellBuilder(mod, "AbilitySpell")
            .AsAbility()
            .Build();

        Assert.Equal(SpellType.Ability, spell.Type);
    }

    #endregion

    #region Weapon Builder Tests

    [Fact]
    public void WeaponBuilder_Build_CreatesWeapon()
    {
        var mod = CreateTestMod();
        var weapon = new WeaponBuilder(mod, "TestWeapon")
            .WithName("Test Sword")
            .WithDamage(25)
            .Build();

        Assert.NotNull(weapon);
        Assert.Equal("TestWeapon", weapon.EditorID);
        Assert.Equal("Test Sword", weapon.Name?.String);
        Assert.Equal(25, weapon.BasicStats!.Damage);
    }

    [Fact]
    public void WeaponBuilder_AsSword_SetsAnimationType()
    {
        var mod = CreateTestMod();
        var weapon = new WeaponBuilder(mod, "SwordWeapon")
            .AsSword()
            .Build();

        Assert.Equal(WeaponAnimationType.OneHandSword, weapon.Data!.AnimationType);
    }

    [Fact]
    public void WeaponBuilder_AsBow_SetsAnimationType()
    {
        var mod = CreateTestMod();
        var weapon = new WeaponBuilder(mod, "BowWeapon")
            .AsBow()
            .Build();

        Assert.Equal(WeaponAnimationType.Bow, weapon.Data!.AnimationType);
    }

    #endregion

    #region Perk Builder Tests

    [Fact]
    public void PerkBuilder_Build_CreatesPerk()
    {
        var mod = CreateTestMod();
        var perk = new PerkBuilder(mod, "TestPerk")
            .WithName("Test Perk")
            .WithDescription("A test perk")
            .Build();

        Assert.NotNull(perk);
        Assert.Equal("TestPerk", perk.EditorID);
        Assert.Equal("Test Perk", perk.Name?.String);
        Assert.Equal("A test perk", perk.Description?.String);
    }

    [Fact]
    public void PerkBuilder_AsPlayable_SetsPlayable()
    {
        var mod = CreateTestMod();
        var perk = new PerkBuilder(mod, "PlayablePerk")
            .AsPlayable()
            .Build();

        Assert.True(perk.Playable);
    }

    [Fact]
    public void PerkBuilder_AsHidden_SetsHidden()
    {
        var mod = CreateTestMod();
        var perk = new PerkBuilder(mod, "HiddenPerk")
            .AsHidden()
            .Build();

        Assert.True(perk.Hidden);
    }

    [Fact]
    public void PerkBuilder_WithWeaponDamageBonus_AddsEffect()
    {
        var mod = CreateTestMod();
        var perk = new PerkBuilder(mod, "DamagePerk")
            .WithWeaponDamageBonus(25)
            .Build();

        Assert.NotEmpty(perk.Effects);
    }

    #endregion

    #region Book Builder Tests

    [Fact]
    public void BookBuilder_Build_CreatesBook()
    {
        var mod = CreateTestMod();
        var book = new BookBuilder(mod, "TestBook")
            .WithName("Ancient Tome")
            .WithText("Once upon a time...")
            .Build();

        Assert.NotNull(book);
        Assert.Equal("TestBook", book.EditorID);
        Assert.Equal("Ancient Tome", book.Name?.String);
        Assert.Contains("Once upon a time", book.BookText?.String);
    }

    [Fact]
    public void BookBuilder_WithValue_SetsValue()
    {
        var mod = CreateTestMod();
        var book = new BookBuilder(mod, "ValueBook")
            .WithValue(500)
            .Build();

        Assert.Equal(500u, book.Value);
    }

    #endregion
}
