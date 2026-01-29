using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Research;

/// <summary>
/// Research file to explore Mutagen Condition API
/// This file is for documentation purposes only
/// </summary>
public class ConditionResearch
{
    public void ExploreConditionReadAPI(SkyrimMod mod)
    {
        // Research: How to ACCESS conditions on different record types

        // Perks - likely have Conditions
        var perk = mod.Perks.First();
        var perkConditions = perk.Conditions; // Type?

        // Research: Spells, Weapons, Armor might not have direct Conditions
        // They might have Effects that have Conditions?
        var spell = mod.Spells.First();
        // Check if effects have conditions
        if (spell.Effects != null && spell.Effects.Count > 0)
        {
            var effect = spell.Effects[0];
            // Does Effect have Conditions?
        }

        var weapon = mod.Weapons.First();
        // Check what properties weapon has

        var armor = mod.Armors.First();
        // Check what properties armor has

        // Research: How to READ condition properties
        if (perkConditions != null && perkConditions.Count > 0)
        {
            var condition = perkConditions[0];

            // Explore IConditionGetter structure
            var data = condition.Data;

            // What properties are available on Data?
            // - ComparisonValue?
            // - CompareOperator?
            // - Flags?
            // - RunOnType?
            // - Function?
            // - Parameters?
        }
    }

    public void ExploreConditionWriteAPI(SkyrimMod mod)
    {
        // Research: How to CREATE new conditions
        var spell = mod.Spells.AddNew("TestSpell");

        // Option 1: Initialize Conditions collection?
        // spell.Conditions = new ExtendedList<Condition>();

        // Option 2: Add to existing collection?
        // var newCondition = new Condition();

        // Research: How to CONFIGURE a condition
        // newCondition.Data = ???
        // - What type is Data?
        // - ConditionData?
        // - How to set Function (GetHasPerk, GetItemCount, etc)?
        // - How to set ComparisonValue?
        // - How to set CompareOperator?
        // - How to set Parameters?

        // Research: How to REMOVE conditions
        // spell.Conditions.Clear();
        // spell.Conditions.RemoveAt(0);
        // spell.Conditions = null;
    }

    public void ExploreConditionDataTypes(SkyrimMod mod)
    {
        // Research: Different condition data types

        // ConditionFloat - for functions with float parameters
        // var floatCondition = new ConditionFloat();

        // ConditionGlobal - for global variable references
        // var globalCondition = new ConditionGlobal();

        // Are there others?
        // How do we know which type to use for which Function?
    }
}
