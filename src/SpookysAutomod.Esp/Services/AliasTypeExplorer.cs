using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda;
using System;
using System.Reflection;
using System.Linq;
using System.IO;

namespace SpookysAutomod.Esp.Services;

/// <summary>
/// Utility to explore QuestAlias types and their properties
/// </summary>
public static class AliasTypeExplorer
{
    public static void InspectEspAliasScripts(string espPath)
    {
        Console.WriteLine($"=== Inspecting Alias Scripts in {Path.GetFileName(espPath)} ===\n");
        
        using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
        using var mod = SkyrimMod.CreateFromBinaryOverlay(espPath, SkyrimRelease.SkyrimSE);
        
        foreach (var quest in mod.Quests)
        {
            Console.WriteLine($"Quest: {quest.EditorID}");
            
            var vma = quest.VirtualMachineAdapter;
            if (vma != null)
            {
                Console.WriteLine($"  VirtualMachineAdapter Type: {vma.GetType().Name}");
                
                // Use interface to access Scripts and Aliases
                if (vma is IQuestAdapterGetter adapter)
                {
                    Console.WriteLine($"  Quest Scripts: {adapter.Scripts.Count}");
                    foreach (var script in adapter.Scripts)
                    {
                        Console.WriteLine($"    - {script.Name} ({script.Properties.Count} properties)");
                        foreach (var prop in script.Properties)
                        {
                            var propValue = prop switch
                            {
                                IScriptObjectPropertyGetter objProp => $"Object -> {objProp.Object.FormKey}",
                                IScriptIntPropertyGetter intProp => $"Int = {intProp.Data}",
                                IScriptFloatPropertyGetter floatProp => $"Float = {floatProp.Data}",
                                IScriptBoolPropertyGetter boolProp => $"Bool = {boolProp.Data}",
                                IScriptStringPropertyGetter strProp => $"String = {strProp.Data}",
                                _ => prop.GetType().Name
                            };
                            Console.WriteLine($"      {prop.Name}: {propValue}");
                        }
                    }
                    
                Console.WriteLine($"  Alias Scripts (QuestFragmentAlias): {adapter.Aliases.Count}");
                foreach (var fragAlias in adapter.Aliases)
                {
                    Console.WriteLine($"    Fragment Alias:");
                    Console.WriteLine($"      Version: {fragAlias.Version}");
                    Console.WriteLine($"      ObjectFormat: {fragAlias.ObjectFormat}");
                    Console.WriteLine($"      Property.Name: {fragAlias.Property?.Name ?? "null"}");
                    Console.WriteLine($"      Property.Alias: {fragAlias.Property?.Alias}");
                    Console.WriteLine($"      Property.Flags: {fragAlias.Property?.Flags}");
                    Console.WriteLine($"      Property.Object: {fragAlias.Property?.Object}");
                    Console.WriteLine($"      Property.Unused: {fragAlias.Property?.Unused}");
                    Console.WriteLine($"      Scripts: {fragAlias.Scripts.Count}");
                        foreach (var script in fragAlias.Scripts)
                        {
                            Console.WriteLine($"        - {script.Name} ({script.Properties.Count} properties)");
                            foreach (var prop in script.Properties)
                            {
                                var propValue = prop switch
                                {
                                    IScriptObjectPropertyGetter objProp => $"Object -> {objProp.Object.FormKey}",
                                    IScriptIntPropertyGetter intProp => $"Int = {intProp.Data}",
                                    IScriptFloatPropertyGetter floatProp => $"Float = {floatProp.Data}",
                                    IScriptBoolPropertyGetter boolProp => $"Bool = {boolProp.Data}",
                                    IScriptStringPropertyGetter strProp => $"String = {strProp.Data}",
                                    _ => prop.GetType().Name
                                };
                                Console.WriteLine($"          {prop.Name}: {propValue}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"  (Not a QuestAdapter - cannot read scripts)");
                }
            }
            else
            {
                Console.WriteLine($"  VirtualMachineAdapter: null");
            }
            
            Console.WriteLine($"  Aliases: {quest.Aliases.Count}");
            foreach (var alias in quest.Aliases)
            {
                Console.WriteLine($"    [{alias.ID}] {alias.Name} - Flags: {alias.Flags}");
            }
            
            Console.WriteLine();
        }
    }

    public static void ExploreQuestAliasTypes()
    {
        var assembly = typeof(Quest).Assembly;
        
        Console.WriteLine("=== Searching for QuestAlias types ===\n");
        
        // Find all types that might be related to QuestAlias
        var aliasTypes = assembly.GetTypes()
            .Where(t => t.Name.Contains("QuestAlias") || 
                       t.Name.Contains("ReferenceAlias") ||
                       t.Name.Contains("LocationAlias"))
            .Where(t => t.Namespace?.Contains("Skyrim") == true)
            .OrderBy(t => t.Name)
            .ToList();
        
        foreach (var type in aliasTypes)
        {
            Console.WriteLine($"Type: {type.FullName}");
            Console.WriteLine($"  IsAbstract: {type.IsAbstract}, IsInterface: {type.IsInterface}");
            Console.WriteLine($"  BaseType: {type.BaseType?.Name}");
            
            // List all public properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.DeclaringType == type) // Only properties defined on this type
                .ToList();
            
            if (props.Count > 0)
            {
                Console.WriteLine($"  Own Properties:");
                foreach (var prop in props)
                {
                    Console.WriteLine($"    - {prop.Name}: {prop.PropertyType.Name}");
                }
            }
            
            // Check for VirtualMachineAdapter specifically
            var vma = type.GetProperty("VirtualMachineAdapter", 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (vma != null)
            {
                Console.WriteLine($"  ** HAS VirtualMachineAdapter: {vma.PropertyType.Name} **");
            }
            
            Console.WriteLine();
        }
        
        // Check QuestAdapter for alias script support
        Console.WriteLine("=== Checking QuestAdapter structure ===\n");
        var adapter = new QuestAdapter();
        Console.WriteLine($"QuestAdapter type: {adapter.GetType().FullName}");
        var adapterProps = adapter.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Console.WriteLine($"Properties:");
        foreach (var prop in adapterProps.OrderBy(p => p.Name))
        {
            Console.WriteLine($"  - {prop.Name}: {prop.PropertyType.Name}");
        }
        
        // Check ScriptEntry for alias index
        Console.WriteLine("\n=== Checking ScriptEntry structure ===\n");
        var scriptEntry = new ScriptEntry();
        var seProps = scriptEntry.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Console.WriteLine($"ScriptEntry properties:");
        foreach (var prop in seProps.OrderBy(p => p.Name))
        {
            Console.WriteLine($"  - {prop.Name}: {prop.PropertyType.Name}");
        }
        
        // Check what type is in the Aliases list
        Console.WriteLine("\n=== Checking QuestAdapter.Aliases element type ===\n");
        var aliasesProperty = typeof(QuestAdapter).GetProperty("Aliases");
        if (aliasesProperty != null)
        {
            var aliasListType = aliasesProperty.PropertyType;
            Console.WriteLine($"Aliases property type: {aliasListType.FullName}");
            
            if (aliasListType.IsGenericType)
            {
                var elementType = aliasListType.GetGenericArguments()[0];
                Console.WriteLine($"Element type: {elementType.FullName}");
                
                // List properties of the element type
                var elemProps = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Console.WriteLine($"Element properties:");
                foreach (var prop in elemProps.OrderBy(p => p.Name))
                {
                    Console.WriteLine($"  - {prop.Name}: {prop.PropertyType.Name}");
                }
            }
        }
        
        // Explore QuestFragmentAlias
        Console.WriteLine("\n=== QuestFragmentAlias structure ===\n");
        var fragAlias = new QuestFragmentAlias();
        Console.WriteLine($"Created: {fragAlias.GetType().FullName}");
        var faProps = fragAlias.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in faProps.OrderBy(p => p.Name))
        {
            Console.WriteLine($"  - {prop.Name}: {prop.PropertyType.FullName}");
        }
        
        // Check ScriptObjectProperty
        Console.WriteLine("\n=== ScriptObjectProperty structure ===\n");
        var objProp = new ScriptObjectProperty();
        var opProps = objProp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in opProps.OrderBy(p => p.Name))
        {
            Console.WriteLine($"  - {prop.Name}: {prop.PropertyType.Name}");
        }
        
        // Demo: How to attach a script to an alias
        Console.WriteLine("\n=== DEMO: Attaching script to alias ===\n");
        Console.WriteLine("// Create a quest");
        Console.WriteLine("var quest = mod.Quests.AddNew(\"MyQuest\");");
        Console.WriteLine("quest.VirtualMachineAdapter = new QuestAdapter();");
        Console.WriteLine("");
        Console.WriteLine("// Create an alias");
        Console.WriteLine("var alias = quest.Aliases.AddNew();");
        Console.WriteLine("alias.Name = \"FollowerAlias01\";");
        Console.WriteLine("alias.ID = 0;");
        Console.WriteLine("alias.Type = QuestAlias.TypeEnum.Reference;");
        Console.WriteLine("");
        Console.WriteLine("// Attach script to alias via QuestFragmentAlias");
        Console.WriteLine("var fragAlias = new QuestFragmentAlias();");
        Console.WriteLine("fragAlias.Property = new ScriptObjectProperty");
        Console.WriteLine("{");
        Console.WriteLine("    Name = \"FollowerAlias01\",");
        Console.WriteLine("    Flags = ScriptProperty.Flag.Edited,");
        Console.WriteLine("    Alias = 0  // Alias index");
        Console.WriteLine("};");
        Console.WriteLine("");
        Console.WriteLine("// Add script to the fragment alias");
        Console.WriteLine("fragAlias.Scripts.Add(new ScriptEntry { Name = \"MyAliasScript\" });");
        Console.WriteLine("");
        Console.WriteLine("// Add to quest adapter");
        Console.WriteLine("var adapter = quest.VirtualMachineAdapter as QuestAdapter;");
        Console.WriteLine("adapter.Aliases.Add(fragAlias);");
    }
}
