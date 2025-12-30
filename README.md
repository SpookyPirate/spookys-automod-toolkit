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
