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

    #region LeveledItem Builder Tests

    [Fact]
    public void LeveledItemBuilder_Build_CreatesLeveledItem()
    {
        var mod = CreateTestMod();
        var leveledItem = new LeveledItemBuilder(mod, "TestLeveledItem")
            .Build();

        Assert.NotNull(leveledItem);
        Assert.Equal("TestLeveledItem", leveledItem.EditorID);
    }

    [Fact]
    public void LeveledItemBuilder_WithChanceNone_SetsChanceNone()
    {
        var mod = CreateTestMod();
        var leveledItem = new LeveledItemBuilder(mod, "ChanceNoneTest")
            .WithChanceNone(25)
            .Build();

        Assert.Equal(0.25, leveledItem.ChanceNone.Value);
    }

    [Fact]
    public void LeveledItemBuilder_AsLowTreasure_SetsPreset()
    {
        var mod = CreateTestMod();
        var leveledItem = new LeveledItemBuilder(mod, "LowTreasure")
            .AsLowTreasure()
            .Build();

        Assert.NotNull(leveledItem);
        Assert.Equal(0.25, leveledItem.ChanceNone.Value);
    }

    [Fact]
    public void LeveledItemBuilder_CalculateForEachItem_SetsFlag()
    {
        var mod = CreateTestMod();
        var leveledItem = new LeveledItemBuilder(mod, "CalcEachTest")
            .CalculateForEachItem()
            .Build();

        Assert.True(leveledItem.Flags.HasFlag(LeveledItem.Flag.CalculateForEachItemInCount));
    }

    #endregion

    #region FormList Builder Tests

    [Fact]
    public void FormListBuilder_Build_CreatesFormList()
    {
        var mod = CreateTestMod();
        var formList = new FormListBuilder(mod, "TestFormList")
            .Build();

        Assert.NotNull(formList);
        Assert.Equal("TestFormList", formList.EditorID);
    }

    [Fact]
    public void FormListBuilder_AddForm_AddsFormToList()
    {
        var mod = CreateTestMod();

        // Create a weapon to add to the form list
        var weapon = new WeaponBuilder(mod, "TestWeapon").Build();

        var formList = new FormListBuilder(mod, "TestFormList")
            .AddForm(weapon.FormKey)
            .Build();

        Assert.NotNull(formList.Items);
        Assert.Single(formList.Items);
    }

    [Fact]
    public void FormListBuilder_AddForms_AddsMultipleForms()
    {
        var mod = CreateTestMod();

        var weapon1 = new WeaponBuilder(mod, "Weapon1").Build();
        var weapon2 = new WeaponBuilder(mod, "Weapon2").Build();

        var formList = new FormListBuilder(mod, "MultiFormList")
            .AddForms(weapon1.FormKey, weapon2.FormKey)
            .Build();

        Assert.NotNull(formList.Items);
        Assert.Equal(2, formList.Items.Count);
    }

    #endregion

    #region EncounterZone Builder Tests

    [Fact]
    public void EncounterZoneBuilder_Build_CreatesEncounterZone()
    {
        var mod = CreateTestMod();
        var encounterZone = new EncounterZoneBuilder(mod, "TestZone")
            .Build();

        Assert.NotNull(encounterZone);
        Assert.Equal("TestZone", encounterZone.EditorID);
        Assert.Equal(1, encounterZone.MinLevel);
        Assert.Equal(0, encounterZone.MaxLevel);
    }

    [Fact]
    public void EncounterZoneBuilder_WithMinLevel_SetsMinLevel()
    {
        var mod = CreateTestMod();
        var encounterZone = new EncounterZoneBuilder(mod, "MinLevelTest")
            .WithMinLevel(10)
            .Build();

        Assert.Equal(10, encounterZone.MinLevel);
    }

    [Fact]
    public void EncounterZoneBuilder_WithMaxLevel_SetsMaxLevel()
    {
        var mod = CreateTestMod();
        var encounterZone = new EncounterZoneBuilder(mod, "MaxLevelTest")
            .WithMinLevel(10)
            .WithMaxLevel(30)
            .Build();

        Assert.Equal(30, encounterZone.MaxLevel);
    }

    [Fact]
    public void EncounterZoneBuilder_NeverResets_SetsFlag()
    {
        var mod = CreateTestMod();
        var encounterZone = new EncounterZoneBuilder(mod, "NeverResetsTest")
            .NeverResets()
            .Build();

        Assert.True(encounterZone.Flags.HasFlag(EncounterZone.Flag.NeverResets));
    }

    [Fact]
    public void EncounterZoneBuilder_AsMidLevel_SetsPreset()
    {
        var mod = CreateTestMod();
        var encounterZone = new EncounterZoneBuilder(mod, "MidLevelTest")
            .AsMidLevel()
            .Build();

        Assert.Equal(10, encounterZone.MinLevel);
        Assert.Equal(30, encounterZone.MaxLevel);
    }

    #endregion

    #region Location Builder Tests

    [Fact]
    public void LocationBuilder_Build_CreatesLocation()
    {
        var mod = CreateTestMod();
        var location = new LocationBuilder(mod, "TestLocation")
            .Build();

        Assert.NotNull(location);
        Assert.Equal("TestLocation", location.EditorID);
    }

    [Fact]
    public void LocationBuilder_WithName_SetsName()
    {
        var mod = CreateTestMod();
        var location = new LocationBuilder(mod, "NamedLocation")
            .WithName("Test Location Name")
            .Build();

        Assert.Equal("Test Location Name", location.Name?.String);
    }

    [Fact]
    public void LocationBuilder_AddKeyword_AddsKeyword()
    {
        var mod = CreateTestMod();

        // Create a keyword to add
        var keyword = mod.Keywords.AddNew("TestKeyword");

        var location = new LocationBuilder(mod, "KeywordLocation")
            .AddKeyword(keyword.FormKey)
            .Build();

        Assert.NotNull(location.Keywords);
        Assert.Single(location.Keywords);
    }

    [Fact]
    public void LocationBuilder_AsInn_AddsInnKeyword()
    {
        var mod = CreateTestMod();
        var location = new LocationBuilder(mod, "InnLocation")
            .AsInn()
            .Build();

        Assert.NotNull(location.Keywords);
        Assert.NotEmpty(location.Keywords);
    }

    #endregion

    #region Outfit Builder Tests

    [Fact]
    public void OutfitBuilder_Build_CreatesOutfit()
    {
        var mod = CreateTestMod();
        var outfit = new OutfitBuilder(mod, "TestOutfit")
            .Build();

        Assert.NotNull(outfit);
        Assert.Equal("TestOutfit", outfit.EditorID);
    }

    [Fact]
    public void OutfitBuilder_AddItem_AddsItemToOutfit()
    {
        var mod = CreateTestMod();

        // Create an armor to add to outfit
        var armor = mod.Armors.AddNew("TestArmor");

        var outfit = new OutfitBuilder(mod, "TestOutfit")
            .AddItem(armor.FormKey)
            .Build();

        Assert.NotNull(outfit.Items);
        Assert.Single(outfit.Items);
    }

    [Fact]
    public void OutfitBuilder_AddItems_AddsMultipleItems()
    {
        var mod = CreateTestMod();

        var armor1 = mod.Armors.AddNew("Armor1");
        var armor2 = mod.Armors.AddNew("Armor2");

        var outfit = new OutfitBuilder(mod, "MultiItemOutfit")
            .AddItems(armor1.FormKey, armor2.FormKey)
            .Build();

        Assert.NotNull(outfit.Items);
        Assert.Equal(2, outfit.Items.Count);
    }

    [Fact]
    public void OutfitBuilder_AsGuard_AddsGuardItems()
    {
        var mod = CreateTestMod();
        var outfit = new OutfitBuilder(mod, "GuardOutfit")
            .AsGuard()
            .Build();

        Assert.NotNull(outfit.Items);
        Assert.NotEmpty(outfit.Items);
    }

    #endregion
}
