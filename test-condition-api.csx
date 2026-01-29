#!/usr/bin/env dotnet-script
#r "nuget: Mutagen.Bethesda, 0.52.0"
#r "nuget: Mutagen.Bethesda.Skyrim, 0.52.0"

using System.Reflection;
using Mutagen.Bethesda.Skyrim;

// Inspect Condition types
Console.WriteLine("=== Condition-related types in Mutagen.Bethesda.Skyrim ===\n");

var assembly = typeof(Perk).Assembly;
var conditionTypes = assembly.GetTypes()
    .Where(t => t.Name.Contains("Condition") || t.Name.Contains("CTDA"))
    .OrderBy(t => t.Name)
    .ToList();

foreach (var type in conditionTypes)
{
    Console.WriteLine($"\n{type.Name} ({type.FullName})");
    Console.WriteLine($"  Base: {type.BaseType?.Name ?? "null"}");
    Console.WriteLine($"  Is Abstract: {type.IsAbstract}");
    Console.WriteLine($"  Is Interface: {type.IsInterface}");

    var interfaces = type.GetInterfaces().Where(i => i.Name.Contains("Condition")).ToList();
    if (interfaces.Any())
    {
        Console.WriteLine($"  Condition Interfaces: {string.Join(", ", interfaces.Select(i => i.Name))}");
    }

    if (!type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToList();
        if (props.Any())
        {
            Console.WriteLine("  Properties:");
            foreach (var prop in props)
            {
                Console.WriteLine($"    {prop.Name}: {prop.PropertyType.Name}");
            }
        }
    }
}

// Check record types
Console.WriteLine("\n\n=== Condition properties on record types ===\n");

var recordTypes = new[] {
    typeof(Spell),
    typeof(Weapon),
    typeof(Armor),
    typeof(Perk),
    typeof(MagicEffect),
    typeof(Ingestible)
};

foreach (var type in recordTypes)
{
    Console.WriteLine($"\n{type.Name}:");
    var conditionProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.Name.Contains("Condition") || p.PropertyType.Name.Contains("Condition"))
        .ToList();

    foreach (var prop in conditionProps)
    {
        Console.WriteLine($"  {prop.Name}: {prop.PropertyType.Name}");

        // Check if it's a collection
        if (prop.PropertyType.IsGenericType)
        {
            var genericArgs = prop.PropertyType.GetGenericArguments();
            Console.WriteLine($"    Generic Args: {string.Join(", ", genericArgs.Select(g => g.Name))}");
        }
    }

    if (!conditionProps.Any())
    {
        Console.WriteLine("  (no condition properties found)");
    }
}
