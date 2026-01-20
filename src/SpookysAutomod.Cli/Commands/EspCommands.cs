using System.CommandLine;
using System.Text.Json;
using SpookysAutomod.Core.Logging;
using SpookysAutomod.Core.Models;

namespace SpookysAutomod.Cli.Commands;

/// <summary>
/// Main entry point for ESP/ESL plugin operations.
/// Commands are organized into separate files for maintainability.
/// </summary>
public static class EspCommands
{
    internal static Option<bool> JsonOption = null!;
    internal static Option<bool> VerboseOption = null!;

    public static Command Create(Option<bool> jsonOption, Option<bool> verboseOption)
    {
        JsonOption = jsonOption;
        VerboseOption = verboseOption;

        var espCommand = new Command("esp", "ESP/ESL plugin operations");

        // Register commands from separate classes
        EspPluginCommands.Register(espCommand);
        EspRecordCommands.Register(espCommand);
        EspScriptCommands.Register(espCommand);
        EspAnalysisCommands.Register(espCommand);

        return espCommand;
    }

    internal static IModLogger CreateLogger(bool json, bool verbose) =>
        json ? new SilentLogger() : new ConsoleLogger(verbose);

    internal static void OutputError(string error, bool json, IEnumerable<string>? suggestions = null)
    {
        if (json)
        {
            Console.WriteLine(Result.Fail(error, suggestions: suggestions?.ToList()).ToJson(true));
        }
        else
        {
            Console.Error.WriteLine($"Error: {error}");
            if (suggestions != null)
            {
                Console.Error.WriteLine("Suggestions:");
                foreach (var s in suggestions)
                    Console.Error.WriteLine($"  - {s}");
            }
        }
        Environment.ExitCode = 1;
    }
}

// Extension for anonymous type JSON
internal static class JsonExtensions
{
    public static string ToJson(this object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
