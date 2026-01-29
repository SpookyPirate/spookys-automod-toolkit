using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Research;

/// <summary>
/// Code examples for working with Conditions in Mutagen.
/// These examples are based on API research and may need adjustment after verification.
/// </summary>
public static class ConditionExamples
{
    /// <summary>
    /// Example 1: Reading conditions from a Perk
    /// VERIFIED: Perks have a Conditions property
    /// </summary>
    public static void ReadPerkConditions(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinaryOverlay(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        foreach (var perk in mod.Perks)
        {
            if (perk.Conditions == null || perk.Conditions.Count == 0)
                continue;

            Console.WriteLine($"Perk: {perk.EditorID}");

            foreach (var condition in perk.Conditions)
            {
                // Access the condition data
                var data = condition.Data;

                // Print condition info
                Console.WriteLine($"  Condition:");
                Console.WriteLine($"    Data Type: {data.GetType().Name}");

                // Common properties (names need verification)
                // Based on PluginService.cs:797-809 commented code:
                // - Function
                // - ComparisonValue
                // - CompareOperator
                // - Flags
                // - RunOnType
                // - ParameterOneRecord, ParameterTwo, etc.

                // Use reflection to show all properties
                foreach (var prop in data.GetType().GetProperties())
                {
                    try
                    {
                        var value = prop.GetValue(data);
                        Console.WriteLine($"    {prop.Name}: {value}");
                    }
                    catch { }
                }
            }
        }
    }

    /// <summary>
    /// Example 2: Checking specific condition types
    /// Based on the 157 condition data types found via debug-types
    /// </summary>
    public static void CheckConditionTypes(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinaryOverlay(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        foreach (var perk in mod.Perks.Where(p => p.Conditions?.Count > 0))
        {
            foreach (var condition in perk.Conditions!)
            {
                var dataType = condition.Data.GetType().Name;

                // Check for specific condition types we discovered
                switch (dataType)
                {
                    case "GetIsAliasRefConditionData":
                        Console.WriteLine($"Found GetIsAliasRef condition on {perk.EditorID}");
                        // Access specific properties for this type
                        break;

                    case "GetInCurrentLocAliasConditionData":
                        Console.WriteLine($"Found GetInCurrentLocAlias condition on {perk.EditorID}");
                        break;

                    // Add more as discovered
                    default:
                        Console.WriteLine($"Unknown condition type: {dataType}");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Example 3: Creating a new condition (UNVERIFIED - needs testing)
    /// This is a hypothetical example based on common Mutagen patterns
    /// </summary>
    public static void CreateConditionExample()
    {
        var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
        var perk = mod.Perks.AddNew("TestPerk");

        // HYPOTHESIS: Creating conditions might look like this
        // This code may not compile until we verify the actual API

        /*
        // Create a condition for "GetHasPerk"
        var condition = new Condition();

        // Set the condition data - each function has its own data type
        condition.Data = new GetHasPerkConditionData
        {
            // Set the perk to check for
            Perk = new FormLink<IPerkGetter>(somePerkFormKey),

            // Comparison value (1.0 = has perk, 0.0 = doesn't have)
            ComparisonValue = 1.0f,

            // Operator (equal to)
            CompareOperator = CompareOperator.EqualTo,

            // Run on the subject
            RunOnType = RunOnType.Subject,

            // Flags
            Flags = ConditionFlags.None
        };

        // Add to perk
        perk.Conditions.Add(condition);
        */

        // The above is HYPOTHETICAL - actual API may differ
    }

    /// <summary>
    /// Example 4: Modifying existing conditions (UNVERIFIED - needs testing)
    /// </summary>
    public static void ModifyConditionExample(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinary(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        var perk = mod.Perks.First(p => p.EditorID == "TestPerk");

        if (perk.Conditions != null && perk.Conditions.Count > 0)
        {
            var condition = perk.Conditions[0];

            // HYPOTHESIS: Modifying might work like this
            // (Actual property names need verification)

            /*
            // Access and modify the data
            var data = condition.Data;

            // Properties we expect based on research:
            // data.ComparisonValue = 2.0f;
            // data.CompareOperator = CompareOperator.GreaterThan;
            // data.Flags |= ConditionFlags.OR;
            */
        }

        mod.WriteToBinary(pluginPath);
    }

    /// <summary>
    /// Example 5: Removing conditions
    /// </summary>
    public static void RemoveConditions(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinary(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        var perk = mod.Perks.First(p => p.EditorID == "TestPerk");

        if (perk.Conditions != null)
        {
            // Remove all conditions
            perk.Conditions.Clear();

            // Or remove specific condition
            // perk.Conditions.RemoveAt(0);

            // Or remove by predicate
            // perk.Conditions.RemoveWhere(c => /* some condition */);
        }

        mod.WriteToBinary(pluginPath);
    }

    /// <summary>
    /// Example 6: Checking which records have conditions
    /// </summary>
    public static void CheckRecordTypesForConditions(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinaryOverlay(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        // Check various record types
        Console.WriteLine("Checking which record types have Conditions...\n");

        // Perks - CONFIRMED to have Conditions
        var perkWithConditions = mod.Perks.FirstOrDefault(p => p.Conditions?.Count > 0);
        Console.WriteLine($"Perk with conditions: {perkWithConditions?.EditorID ?? "none found"}");

        // Spells - Need to verify
        // var spellWithConditions = mod.Spells.FirstOrDefault(s => s.Conditions?.Count > 0);
        // Console.WriteLine($"Spell with conditions: {spellWithConditions?.EditorID ?? "none found"}");

        // Weapons - Need to verify
        // var weaponWithConditions = mod.Weapons.FirstOrDefault(w => w.Conditions?.Count > 0);
        // Console.WriteLine($"Weapon with conditions: {weaponWithConditions?.EditorID ?? "none found"}");

        // Armor - Need to verify
        // var armorWithConditions = mod.Armors.FirstOrDefault(a => a.Conditions?.Count > 0);
        // Console.WriteLine($"Armor with conditions: {armorWithConditions?.EditorID ?? "none found"}");

        // MagicEffect - Need to verify
        // var effectWithConditions = mod.MagicEffects.FirstOrDefault(m => m.Conditions?.Count > 0);
        // Console.WriteLine($"MagicEffect with conditions: {effectWithConditions?.EditorID ?? "none found"}");
    }

    /// <summary>
    /// Example 7: Inspect condition parameters
    /// Based on GetIsAliasRefConditionData showing properties:
    /// - Reference: IFormLink<ISkyrimMajorRecordGetter>?
    /// - ReferenceAliasIndex: Int32
    /// - RunOnType: RunOnType
    /// - UseAliases: Boolean
    /// </summary>
    public static void InspectConditionParameters(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinaryOverlay(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        foreach (var perk in mod.Perks.Where(p => p.Conditions?.Count > 0))
        {
            Console.WriteLine($"\nPerk: {perk.EditorID}");

            foreach (var condition in perk.Conditions!)
            {
                var data = condition.Data;
                var dataType = data.GetType();

                Console.WriteLine($"  Condition Type: {dataType.Name}");

                // Look for FormLink properties (parameters pointing to other records)
                var formLinkProps = dataType.GetProperties()
                    .Where(p => p.PropertyType.Name.Contains("FormLink"));

                foreach (var prop in formLinkProps)
                {
                    var value = prop.GetValue(data);
                    Console.WriteLine($"    {prop.Name}: {value}");
                }

                // Look for index properties (alias indices, etc.)
                var indexProps = dataType.GetProperties()
                    .Where(p => p.Name.Contains("Index") && p.PropertyType == typeof(int));

                foreach (var prop in indexProps)
                {
                    var value = prop.GetValue(data);
                    Console.WriteLine($"    {prop.Name}: {value}");
                }
            }
        }
    }

    /// <summary>
    /// Example 8: Get all unique condition types used in a plugin
    /// </summary>
    public static Dictionary<string, int> GetConditionTypeStatistics(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinaryOverlay(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        var conditionCounts = new Dictionary<string, int>();

        foreach (var perk in mod.Perks.Where(p => p.Conditions?.Count > 0))
        {
            foreach (var condition in perk.Conditions!)
            {
                var typeName = condition.Data.GetType().Name;

                if (!conditionCounts.ContainsKey(typeName))
                    conditionCounts[typeName] = 0;

                conditionCounts[typeName]++;
            }
        }

        return conditionCounts;
    }
}
