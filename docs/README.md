# Spooky's AutoMod Toolkit

A comprehensive LLM-friendly CLI toolkit for autonomous Skyrim mod creation.

## Overview

This toolkit enables LLMs and developers to create and edit Skyrim mods programmatically:

- **ESP/ESL Plugins** - Create and modify plugin files with quests, spells, globals, and scripts
- **Papyrus Scripts** - Compile, decompile, validate, and generate Papyrus scripts
- **NIF Meshes** - Inspect mesh files, extract textures, scale models
- **BSA/BA2 Archives** - Create and extract game archives
- **MCM Configuration** - Generate MCM Helper JSON configurations
- **Audio Files** - Convert and manipulate FUZ, XWM, and WAV audio
- **SKSE Plugins** - Generate C++ SKSE plugin projects with CommonLibSSE-NG

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- Windows (required for some external tools)

### Build from Source

```bash
git clone https://github.com/SpookyPirate/spookys-automod-toolkit.git
cd spookys-automod-toolkit
dotnet build
```

### Run the CLI

```bash
dotnet run --project src/SpookysAutomod.Cli -- <command>
```

Or build and run the executable directly:

```bash
dotnet publish -c Release
./src/SpookysAutomod.Cli/bin/Release/net8.0/spookys-automod <command>
```

## Quick Start

```bash
# Create a new light plugin
spookys-automod esp create "MyMod" --light --author "YourName"

# Add a quest
spookys-automod esp add-quest "MyMod.esp" "MyQuest" --name "My Quest" --start-enabled

# Generate a script template
spookys-automod papyrus generate --name "MyScript" --extends "Quest" --output ./Scripts

# Create MCM configuration
spookys-automod mcm create "MyMod" "My Mod Settings" --output ./MCM/config.json

# Generate SKSE plugin project
spookys-automod skse create "MyPlugin" --template papyrus-native --author "YourName"
```

## Modules

### ESP Module

Create and manipulate ESP/ESL plugin files using Mutagen.

```bash
spookys-automod esp create <name> [--light] [--author <name>]
spookys-automod esp info <plugin>
spookys-automod esp add-quest <plugin> <editorId> [--name <name>] [--start-enabled]
spookys-automod esp add-spell <plugin> <editorId> [--name <name>] [--type <type>]
spookys-automod esp add-global <plugin> <editorId> [--type <type>] [--value <value>]
spookys-automod esp attach-script <plugin> --quest <id> --script <name>
spookys-automod esp generate-seq <plugin> --output <dir>
```

### Papyrus Module

Compile, decompile, and manage Papyrus scripts.

```bash
spookys-automod papyrus status                    # Check tool availability
spookys-automod papyrus download                  # Download compiler/decompiler
spookys-automod papyrus compile <source> --output <dir> --headers <dir>
spookys-automod papyrus decompile <pex> --output <dir>
spookys-automod papyrus validate <psc>
spookys-automod papyrus generate --name <name> --extends <type> --output <dir>
```

### NIF Module

Inspect and manipulate NIF mesh files.

```bash
spookys-automod nif info <file>
spookys-automod nif textures <file>
spookys-automod nif scale <file> <factor> --output <file>
spookys-automod nif copy <file> --output <file>
```

### Archive Module

Create and extract BSA/BA2 archives.

```bash
spookys-automod archive info <archive>
spookys-automod archive list <archive>
spookys-automod archive extract <archive> --output <dir>
spookys-automod archive create <directory> --output <file> [--compress]
```

### MCM Module

Generate MCM Helper configuration files.

```bash
spookys-automod mcm create <modName> <displayName> --output <file>
spookys-automod mcm info <config>
spookys-automod mcm validate <config>
spookys-automod mcm add-toggle <config> <id> <text> [--help-text <text>]
spookys-automod mcm add-slider <config> <id> <text> --min <n> --max <n> [--step <n>]
```

### Audio Module

Convert and manipulate audio files.

```bash
spookys-automod audio info <file>
spookys-automod audio extract-fuz <fuz> --output <dir>
spookys-automod audio create-fuz <xwm> --lip <lip> --output <fuz>
spookys-automod audio wav-to-xwm <wav> --output <xwm>
```

### SKSE Module

Generate SKSE C++ plugin projects.

```bash
spookys-automod skse templates                     # List available templates
spookys-automod skse create <name> --template <template> [--author <name>]
spookys-automod skse info <project>
spookys-automod skse add-function <project> --name <func> --return <type> [--param <type:name>]
```

**Available Templates:**
- `basic` - Simple SKSE plugin with event handlers
- `papyrus-native` - SKSE plugin with Papyrus native function support

## JSON Output

All commands support `--json` flag for structured output, ideal for LLM parsing:

```bash
spookys-automod esp info "MyMod.esp" --json
```

```json
{
  "success": true,
  "result": {
    "fileName": "MyMod.esp",
    "isLight": true,
    "author": "YourName",
    "totalRecords": 5
  }
}
```

Error responses include context and suggestions:

```json
{
  "success": false,
  "error": "Compilation failed",
  "errorContext": "Missing EndFunction at line 15",
  "suggestions": ["Add 'EndFunction' after line 20"]
}
```

## External Tools

Some modules require external tools that are automatically downloaded on first use:

| Tool | Source | Purpose |
|------|--------|---------|
| papyrus-compiler | [russo-2025](https://github.com/russo-2025/papyrus-compiler) | Compile PSC to PEX |
| Champollion | [Orvid](https://github.com/Orvid/Champollion) | Decompile PEX to PSC |

Tools are stored in the `tools/` directory and auto-detected.

## License

MIT License - See LICENSE file for details.
