using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Reflection;

namespace ConditionApiTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Mutagen Condition API Investigation ===\n");

        // Part 1: Type Inspection
        Console.WriteLine("PART 1: TYPE INSPECTION");
        Console.WriteLine("========================\n");

        InspectConditionTypes();

        // Part 2: Test with new plugin (no game env needed)
        Console.WriteLine("\n\nPART 2: PLUGIN CREATION TEST");
        Console.WriteLine("==============================\n");

        TestWithNewPlugin();

        // Part 3: API Pattern Tests
        Console.WriteLine("\n\nPART 3: API PATTERN TESTS");
        Console.WriteLine("===========================\n");

        TestConditionCreationPatterns();

        Console.WriteLine("\n\n=== Test Complete ===");
    }

    static void InspectConditionTypes()
    {
        var assembly = typeof(ISkyrimMod).Assembly;

        // Find all types with "Condition" in the name
        var conditionTypes = assembly.GetTypes()
            .Where(t => t.Name.Contains("Condition") && t.IsPublic)
            .OrderBy(t => t.Name)
            .ToList();

        Console.WriteLine($"Found {conditionTypes.Count} Condition-related types:\n");

        foreach (var type in conditionTypes.Take(20)) // Limit output
        {
            Console.WriteLine($"  - {type.Name} ({type.Namespace})");

            if (type.IsInterface || type.IsClass)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                if (props.Length > 0 && props.Length < 15)
                {
                    foreach (var prop in props)
                    {
                        Console.WriteLine($"      {prop.Name}: {prop.PropertyType.Name}");
                    }
                }
            }
            Console.WriteLine();
        }
    }


    static void InspectConditionObject(object condition, string indent)
    {
        var type = condition.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Console.WriteLine($"{indent}Condition properties:");
        foreach (var prop in props)
        {
            try
            {
                var value = prop.GetValue(condition);
                Console.WriteLine($"{indent}  {prop.Name}: {value} ({prop.PropertyType.Name})");
            }
            catch
            {
                Console.WriteLine($"{indent}  {prop.Name}: <error reading> ({prop.PropertyType.Name})");
            }
        }
    }

    static void TestWithNewPlugin()
    {
        Console.WriteLine("Creating test plugin to explore API...\n");

        var mod = new SkyrimMod(ModKey.FromFileName("ConditionTest.esp"), SkyrimRelease.SkyrimSE);

        // Try to create a perk with conditions
        var perk = mod.Perks.AddNew();
        perk.EditorID = "TestPerk";

        Console.WriteLine($"Created perk: {perk.EditorID}");

        // Check if perk has Conditions property
        var perkType = perk.GetType();
        var conditionsProp = perkType.GetProperty("Conditions");

        if (conditionsProp != null)
        {
            Console.WriteLine($"✓ Perk has Conditions property: {conditionsProp.PropertyType.FullName}");

            // Try to get the collection
            var conditions = conditionsProp.GetValue(perk);
            Console.WriteLine($"  Current value: {conditions}");
            Console.WriteLine($"  Type: {conditions?.GetType().FullName ?? "null"}");
        }
        else
        {
            Console.WriteLine($"✗ Perk does not have Conditions property");
        }
    }

    static void TestConditionCreationPatterns()
    {
        Console.WriteLine("Testing different condition creation patterns:\n");

        // Pattern 1: Direct constructor
        Console.WriteLine("Pattern 1: Direct 'new Condition()'");
        try
        {
            var assembly = typeof(ISkyrimMod).Assembly;
            var conditionType = assembly.GetType("Mutagen.Bethesda.Skyrim.Condition");

            if (conditionType != null)
            {
                var constructor = conditionType.GetConstructor(Type.EmptyTypes);
                if (constructor != null)
                {
                    var condition = constructor.Invoke(null);
                    Console.WriteLine($"  ✓ Created: {condition.GetType().Name}");
                    InspectConditionObject(condition, "    ");
                }
                else
                {
                    Console.WriteLine($"  ✗ No parameterless constructor found");
                }
            }
            else
            {
                Console.WriteLine($"  ✗ Type 'Condition' not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
        }

        // Pattern 2: Look for factory methods
        Console.WriteLine("\nPattern 2: Looking for factory methods");
        try
        {
            var assembly = typeof(ISkyrimMod).Assembly;
            var types = assembly.GetTypes()
                .Where(t => t.Name.Contains("Condition") && t.IsClass && !t.IsAbstract)
                .ToList();

            foreach (var type in types.Take(5))
            {
                Console.WriteLine($"\n  Type: {type.Name}");
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name.Contains("Create") || m.Name.Contains("New"))
                    .ToList();

                if (methods.Any())
                {
                    foreach (var method in methods)
                    {
                        Console.WriteLine($"    - {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
        }

        // Pattern 3: Check what collection type is used
        Console.WriteLine("\nPattern 3: Check collection type for Conditions");
        try
        {
            var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
            var perk = mod.Perks.AddNew();

            var perkType = perk.GetType();
            var conditionsProp = perkType.GetProperty("Conditions");

            if (conditionsProp != null)
            {
                var collectionType = conditionsProp.PropertyType;
                Console.WriteLine($"  Collection type: {collectionType.FullName}");

                // Check if it has Add method
                var addMethod = collectionType.GetMethod("Add");
                if (addMethod != null)
                {
                    Console.WriteLine($"  ✓ Has Add method");
                    var paramType = addMethod.GetParameters()[0].ParameterType;
                    Console.WriteLine($"    Parameter type: {paramType.FullName}");
                }

                // Check if it has Clear method
                var clearMethod = collectionType.GetMethod("Clear");
                if (clearMethod != null)
                {
                    Console.WriteLine($"  ✓ Has Clear method");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error: {ex.Message}");
        }
    }
}
