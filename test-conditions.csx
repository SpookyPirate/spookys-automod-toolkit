#!/usr/bin/env dotnet-script
#r "src/SpookysAutomod.Esp/bin/Debug/net8.0/Mutagen.Bethesda.Kernel.dll"
#r "src/SpookysAutomod.Esp/bin/Debug/net8.0/Mutagen.Bethesda.dll"
#r "src/SpookysAutomod.Esp/bin/Debug/net8.0/Mutagen.Bethesda.Skyrim.dll"

using System.Reflection;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;

// Create a test perk
var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
var perk = mod.Perks.AddNew("TestPerk");

Console.WriteLine("=== Perk Properties ===\n");
foreach (var prop in typeof(Perk).GetProperties().Where(p => p.Name.Contains("Condition")))
{
    Console.WriteLine($"{prop.Name}: {prop.PropertyType.FullName}");

    // Try to get the value
    var value = prop.GetValue(perk);
    Console.WriteLine($"  Current value: {value}");
    Console.WriteLine($"  Can write: {prop.CanWrite}");

    if (prop.PropertyType.IsGenericType)
    {
        var genericArgs = prop.PropertyType.GetGenericArguments();
        Console.WriteLine($"  Generic type args: {string.Join(", ", genericArgs.Select(t => t.Name))}");

        // Get the element type
        if (genericArgs.Length > 0)
        {
            var elementType = genericArgs[0];
            Console.WriteLine($"\n  === {elementType.Name} Properties ===");
            foreach (var elementProp in elementType.GetProperties())
            {
                Console.WriteLine($"    {elementProp.Name}: {elementProp.PropertyType.Name}");
            }
        }
    }

    Console.WriteLine();
}

// Try to explore what we can create
Console.WriteLine("\n=== Available Condition Types ===\n");
var assembly = typeof(Perk).Assembly;
var types = assembly.GetTypes()
    .Where(t => t.Name.Contains("Condition") && !t.IsAbstract && !t.IsInterface && t.IsPublic)
    .Where(t => !t.Name.Contains("Data") && !t.Name.Contains("BinaryTranslation") && !t.Name.Contains("MixIn"))
    .OrderBy(t => t.Name)
    .Take(20);

foreach (var type in types)
{
    Console.WriteLine($"{type.Name} - {type.FullName}");
}
