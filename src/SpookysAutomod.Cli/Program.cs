using System.CommandLine;
using SpookysAutomod.Cli.Commands;

// Root command
var rootCommand = new RootCommand("Spooky's AutoMod Toolkit - LLM-friendly Skyrim mod creation");

// Add global options
var jsonOption = new Option<bool>(
    aliases: new[] { "--json", "-j" },
    description: "Output results as JSON for machine parsing");
rootCommand.AddGlobalOption(jsonOption);

var verboseOption = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "Enable verbose output");
rootCommand.AddGlobalOption(verboseOption);

// Add subcommands with shared options
rootCommand.AddCommand(EspCommands.Create(jsonOption, verboseOption));
rootCommand.AddCommand(PapyrusCommands.Create(jsonOption, verboseOption));

// Run
return await rootCommand.InvokeAsync(args);
