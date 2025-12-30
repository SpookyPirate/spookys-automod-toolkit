# Spookys AutoMod Toolkit

A comprehensive LLM-friendly CLI toolkit for autonomous Skyrim mod creation.

## Features

- **ESP/ESL Plugins** - Create and modify Skyrim plugins with Mutagen
- **Papyrus Scripts** - Compile, decompile, validate, and generate scripts
- **NIF Meshes** - Inspect mesh files, extract textures, scale models
- **BSA/BA2 Archives** - Create and extract game archives
- **MCM Configuration** - Generate MCM Helper JSON configurations
- **Audio Files** - Convert and manipulate FUZ, XWM, and WAV audio
- **SKSE Plugins** - Generate C++ SKSE plugin projects with CommonLibSSE-NG

## What It Can Do

- Create new ESP/ESL plugins from scratch with quests, spells, globals, and script attachments
- Generate Papyrus script templates and compile/decompile scripts
- Read NIF mesh headers, list referenced textures, and perform basic scaling
- Read BSA/BA2 archive headers and list contents
- Generate complete MCM Helper JSON configurations with toggles and sliders
- Parse FUZ audio files and extract XWM/LIP components
- Scaffold complete SKSE C++ plugin projects ready for compilation
- Provide LLM-friendly JSON output for all operations
- Auto-download required external tools (papyrus-compiler, Champollion)

## What It Can't Do

- **No visual editing** - This is a CLI tool, not a replacement for xEdit or Creation Kit
- **No complex record types** - Currently limited to quests, spells, globals; no NPCs, weapons, armors, dialogue, etc.
- **No NIF editing** - Can read headers and scale, but cannot modify meshes, add nodes, or edit UVs
- **No BSA/BA2 creation** - Can read archives but creation is not yet implemented
- **No texture handling** - Cannot read, convert, or modify DDS/PNG textures
- **No FaceGen** - Cannot generate face meshes or tint masks
- **No navmesh** - Cannot create or modify navigation meshes
- **No worldspace/cell editing** - Cannot place objects, create interiors, or modify landscapes
- **No LOD generation** - Cannot create terrain or object LODs
- **No ESP merging** - Cannot merge or patch multiple plugins
- **No dependency resolution** - Does not automatically handle master file dependencies
- **No SKSE compilation** - Generates project files but requires user to have CMake/MSVC to build

## Quick Start

```bash
# Build
dotnet build

# Run
dotnet run --project src/SpookysAutomod.Cli -- <command>

# Examples
spookys-automod esp create "MyMod" --light --author "YourName"
spookys-automod papyrus generate --name "MyScript" --extends "Quest" --output ./Scripts
spookys-automod skse create "MyPlugin" --template papyrus-native
```

## Documentation

- [Full Documentation](docs/README.md)
- [LLM Usage Guide](docs/llm-guide.md)

## JSON Output

All commands support --json for structured output:

```bash
spookys-automod esp info "MyMod.esp" --json
```

## Modules

| Module | Description |
|--------|-------------|
| esp | ESP/ESL plugin creation and modification |
| papyrus | Script compilation, decompilation, validation |
| nif | NIF mesh file operations |
| archive | BSA/BA2 archive handling |
| mcm | MCM Helper configuration generation |
| audio | FUZ/XWM/WAV audio conversion |
| skse | SKSE C++ plugin project generation |

## Requirements

- .NET 8.0 SDK
- Windows (for external tools)

## License

MIT License
