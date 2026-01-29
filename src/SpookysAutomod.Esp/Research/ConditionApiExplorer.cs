using System.Reflection;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace SpookysAutomod.Esp.Research;

/// <summary>
/// Explores the Mutagen Condition API using reflection and code examples
/// </summary>
public static class ConditionApiExplorer
{
    /// <summary>
    /// Example 1: Reading conditions from a Perk
    /// </summary>
    public static void ReadConditionsExample(string pluginPath)
    {
        var mod = SkyrimMod.CreateFromBinaryOverlay(
            ModPath.FromPath(pluginPath),
            SkyrimRelease.SkyrimSE);

        foreach (var perk in mod.Perks)
        {
            if (perk.Conditions != null && perk.Conditions.Count > 0)
            {
                Console.WriteLine($"Perk: {perk.EditorID}");

                foreach (var condition in perk.Conditions)
                {
                    // Access condition properties
                    var data = condition.Data;

                    // Log the structure
                    Console.WriteLine($"  Condition Type: {condition.GetType().Name}");
                    Console.WriteLine($"  Data Type: {data.GetType().Name}");
                    Console.WriteLine($"  Flags: {condition.Flags}");

                    // Try to access data properties
                    Console.WriteLine($"  Data properties:");
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
    }

    /// <summary>
    /// Example 2: Creating a new condition
    /// </summary>
    public static void CreateConditionExample()
    {
        var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
        var perk = mod.Perks.AddNew("TestPerk");

        // Try to create a condition
        // var condition = new Condition();
        // var conditionFloat = new ConditionFloat();
        // var conditionGlobal = new ConditionGlobal();

        // perk.Conditions = new ExtendedList<Condition>();
        // perk.Conditions.Add(condition);
    }

    /// <summary>
    /// Inspects the Condition type hierarchy
    /// </summary>
    public static void InspectConditionTypes()
    {
        var assembly = typeof(Perk).Assembly;

        Console.WriteLine("=== Condition-related types in Mutagen.Bethesda.Skyrim ===\n");

        var conditionTypes = assembly.GetTypes()
            .Where(t => t.Name.Contains("Condition") || t.Name.Contains("CTDA"))
            .OrderBy(t => t.Name);

        foreach (var type in conditionTypes)
        {
            Console.WriteLine($"{type.FullName}");
            Console.WriteLine($"  Base: {type.BaseType?.Name}");
            Console.WriteLine($"  Interfaces: {string.Join(", ", type.GetInterfaces().Select(i => i.Name))}");

            if (!type.IsAbstract && !type.IsInterface)
            {
                Console.WriteLine("  Properties:");
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Console.WriteLine($"    {prop.Name}: {prop.PropertyType.Name}");
                }
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Inspects what properties records have for conditions
    /// </summary>
    public static void InspectRecordConditionProperties()
    {
        Console.WriteLine("=== Condition properties on record types ===\n");

        var recordTypes = new[] {
            typeof(Spell),
            typeof(Weapon),
            typeof(Armor),
            typeof(Perk),
            typeof(MagicEffect)
        };

        foreach (var type in recordTypes)
        {
            Console.WriteLine($"{type.Name}:");
            var conditionProps = type.GetProperties()
                .Where(p => p.Name.Contains("Condition") || p.PropertyType.Name.Contains("Condition"));

            foreach (var prop in conditionProps)
            {
                Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");

                // Show generic args if present
                if (prop.PropertyType.IsGenericType)
                {
                    var genericArgs = prop.PropertyType.GetGenericArguments();
                    Console.WriteLine($"    Generic args: {string.Join(", ", genericArgs.Select(g => g.Name))}");
                }
            }

            if (!conditionProps.Any())
            {
                Console.WriteLine("  (no condition properties found)");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Test creating conditions with sample code
    /// </summary>
    public static void TestCreatingConditions()
    {
        Console.WriteLine("=== Testing Condition Creation ===\n");

        var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
        var perk = mod.Perks.AddNew("TestPerk");

        // Check what Conditions property is
        Console.WriteLine($"Perk.Conditions type: {perk.Conditions?.GetType().FullName ?? "null"}");
        Console.WriteLine($"Perk.Conditions count: {perk.Conditions?.Count ?? 0}");

        // Try to explore the element type
        if (perk.Conditions != null)
        {
            var collectionType = perk.Conditions.GetType();
            if (collectionType.IsGenericType)
            {
                var elementType = collectionType.GetGenericArguments()[0];
                Console.WriteLine($"Element type: {elementType.FullName}");
                Console.WriteLine($"\nElement properties:");
                foreach (var prop in elementType.GetProperties())
                {
                    Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");
                }
            }
        }
    }
}
