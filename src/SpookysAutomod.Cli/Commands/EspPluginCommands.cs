using System.CommandLine;
using Mutagen.Bethesda.Skyrim;
using SpookysAutomod.Esp.Services;

namespace SpookysAutomod.Cli.Commands;

/// <summary>
/// Plugin file operations: create, info, list-masters, merge
/// </summary>
internal static class EspPluginCommands
{
    public static void Register(Command parent)
    {
        parent.AddCommand(CreateCreateCommand());
        parent.AddCommand(CreateInfoCommand());
        parent.AddCommand(CreateListMastersCommand());
        parent.AddCommand(CreateMergeCommand());
    }

    private static Command CreateCreateCommand()
    {
        var nameArg = new Argument<string>("name", "Name of the plugin file (e.g., MyMod.esp)");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => ".",
            description: "Output directory");
        var lightOption = new Option<bool>("--light", "Create as a light plugin (ESL flagged)");
        var authorOption = new Option<string?>("--author", "Author name for the plugin header");
        var descOption = new Option<string?>("--description", "Description for the plugin header");

        var cmd = new Command("create", "Create a new ESP/ESL plugin")
        {
            nameArg,
            outputOption,
            lightOption,
            authorOption,
            descOption
        };

        cmd.SetHandler((name, output, light, author, desc, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.CreatePlugin(name, output, light, author, desc);

            if (json)
            {
                Console.WriteLine(result.ToJson(true));
            }
            else if (result.Success)
            {
                Console.WriteLine($"Created plugin: {result.Value}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, nameArg, outputOption, lightOption, authorOption, descOption,
           EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateInfoCommand()
    {
        var pathArg = new Argument<string>("plugin", "Path to the plugin file");

        var cmd = new Command("info", "Get information about a plugin")
        {
            pathArg
        };

        cmd.SetHandler((path, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.GetPluginInfo(path);

            if (json)
            {
                Console.WriteLine(result.ToJson(true));
            }
            else if (result.Success && result.Value != null)
            {
                var info = result.Value;
                Console.WriteLine($"Plugin: {info.FileName}");
                Console.WriteLine($"Path: {info.FilePath}");
                Console.WriteLine($"Size: {info.FileSize:N0} bytes");
                Console.WriteLine($"Light: {info.IsLight}");
                Console.WriteLine($"Master: {info.IsMaster}");

                if (!string.IsNullOrEmpty(info.Author))
                    Console.WriteLine($"Author: {info.Author}");

                if (info.MasterFiles.Count > 0)
                {
                    Console.WriteLine($"Masters: {string.Join(", ", info.MasterFiles)}");
                }

                Console.WriteLine($"\nRecords ({info.TotalRecords} total):");
                foreach (var (type, count) in info.RecordCounts.Where(kv => kv.Value > 0))
                {
                    Console.WriteLine($"  {type}: {count}");
                }
            }
            else
            {
                Console.Error.WriteLine($"Error: {result.Error}");
                Environment.ExitCode = 1;
            }
        }, pathArg, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateListMastersCommand()
    {
        var pluginArg = new Argument<string>("plugin", "Path to the plugin file");

        var cmd = new Command("list-masters", "List master file dependencies")
        {
            pluginArg
        };

        cmd.SetHandler((plugin, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var result = service.GetPluginInfo(plugin);

            if (json)
            {
                Console.WriteLine(new { success = result.Success, result = new { masters = result.Value?.MasterFiles }, error = result.Error }.ToJson());
            }
            else if (result.Success && result.Value != null)
            {
                Console.WriteLine($"Master files for {result.Value.FileName}:");
                if (result.Value.MasterFiles.Count == 0)
                    Console.WriteLine("  (none)");
                else
                    foreach (var master in result.Value.MasterFiles)
                        Console.WriteLine($"  - {master}");
            }
            else
            { Console.Error.WriteLine($"Error: {result.Error}"); Environment.ExitCode = 1; }
        }, pluginArg, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }

    private static Command CreateMergeCommand()
    {
        var sourceArg = new Argument<string>("source", "Source plugin to merge from");
        var targetArg = new Argument<string>("target", "Target plugin to merge into");
        var outputOption = new Option<string?>("--output", "Output path (defaults to overwriting target)");

        var cmd = new Command("merge", "Merge records from one plugin into another")
        {
            sourceArg, targetArg, outputOption
        };

        cmd.SetHandler((source, target, output, json, verbose) =>
        {
            var logger = EspCommands.CreateLogger(json, verbose);
            var service = new PluginService(logger);

            var sourceResult = service.LoadPluginReadOnly(source);
            if (!sourceResult.Success) { EspCommands.OutputError(sourceResult.Error!, json); return; }

            var targetResult = service.LoadPluginForEdit(target);
            if (!targetResult.Success) { EspCommands.OutputError(targetResult.Error!, json); return; }

            var sourceMod = sourceResult.Value!;
            var targetMod = targetResult.Value!;

            int recordsCopied = 0;

            // Copy all major record types using DeepCopy
            foreach (var quest in sourceMod.Quests) { targetMod.Quests.Add(quest.DeepCopy()); recordsCopied++; }
            foreach (var spell in sourceMod.Spells) { targetMod.Spells.Add(spell.DeepCopy()); recordsCopied++; }
            foreach (var global in sourceMod.Globals) { targetMod.Globals.Add(global.DeepCopy()); recordsCopied++; }
            foreach (var weapon in sourceMod.Weapons) { targetMod.Weapons.Add(weapon.DeepCopy()); recordsCopied++; }
            foreach (var armor in sourceMod.Armors) { targetMod.Armors.Add(armor.DeepCopy()); recordsCopied++; }
            foreach (var npc in sourceMod.Npcs) { targetMod.Npcs.Add(npc.DeepCopy()); recordsCopied++; }
            foreach (var book in sourceMod.Books) { targetMod.Books.Add(book.DeepCopy()); recordsCopied++; }
            foreach (var perk in sourceMod.Perks) { targetMod.Perks.Add(perk.DeepCopy()); recordsCopied++; }

            var saveResult = service.SavePlugin(targetMod, output ?? target);

            if (json)
                Console.WriteLine(new { success = saveResult.Success, result = new { recordsCopied, outputPath = output ?? target }, error = saveResult.Error }.ToJson());
            else if (saveResult.Success)
                Console.WriteLine($"Merged {recordsCopied} records from {source} into {output ?? target}");
            else
            { Console.Error.WriteLine($"Error: {saveResult.Error}"); Environment.ExitCode = 1; }
        }, sourceArg, targetArg, outputOption, EspCommands.JsonOption, EspCommands.VerboseOption);

        return cmd;
    }
}
