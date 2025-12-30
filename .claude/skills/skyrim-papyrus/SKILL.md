---
name: skyrim-papyrus
description: Compile, decompile, validate, and generate Papyrus scripts. Use when the user wants to write scripts, understand script behavior, fix script errors, or generate script templates for quests, actors, or magic effects.
---

# Skyrim Papyrus Module

Compile, decompile, validate, and generate Papyrus scripts using Spooky's AutoMod Toolkit.

## Prerequisites

Run all commands from the toolkit directory:
```bash
cd "C:\Users\spook\Desktop\Projects\3. Development\skyrim-mods\spookys-automod-toolkit"
```

## External Tools

Tools are auto-downloaded on first use:
| Tool | Purpose |
|------|---------|
| papyrus-compiler | Compiles PSC to PEX |
| Champollion | Decompiles PEX to PSC |

Check tool status:
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus status
```

## Command Reference

### Check Tool Status
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus status
```

### Download Tools
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus download
```

### Compile Scripts
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus compile "<source>" --output "<dir>" --headers "<dir>"
```
| Option | Description |
|--------|-------------|
| `<source>` | PSC file or directory |
| `--output`, `-o` | Output directory for PEX files |
| `--headers`, `-i` | Directory containing script headers |
| `--optimize` | Enable optimization (default: true) |

**Headers Path Examples:**
- Steam: `Steam/steamapps/common/Skyrim Special Edition/Data/Scripts/Source`
- With CK: `Steam/steamapps/common/Skyrim Special Edition/Data/Source/Scripts`

### Decompile Scripts
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus decompile "<pex>" --output "<dir>"
```
| Option | Description |
|--------|-------------|
| `<pex>` | PEX file or directory |
| `--output`, `-o` | Output directory for PSC files |

### Validate Script Syntax
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus validate "<psc>"
```

### Generate Script Template
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus generate --name "<name>" --extends "<type>" --output "<dir>"
```
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | Required | Script name (no extension) |
| `--extends` | `Quest` | Base type to extend |
| `--output`, `-o` | `.` | Output directory |
| `--description` | - | Description comment |

**Base Types:**
- `Quest` - Quest scripts
- `Actor` - Actor scripts
- `ObjectReference` - Object reference scripts
- `MagicEffect` - Magic effect scripts
- `ActiveMagicEffect` - Active magic effect scripts
- `Alias` - Alias scripts
- `ReferenceAlias` - Reference alias scripts
- `LocationAlias` - Location alias scripts

## Common Workflows

### Create and Compile a Quest Script
```bash
# 1. Generate script template
dotnet run --project src/SpookysAutomod.Cli -- papyrus generate --name "MyMod_QuestScript" --extends Quest --output "./Scripts/Source"

# 2. Edit the generated script (add your code)

# 3. Validate syntax before compiling
dotnet run --project src/SpookysAutomod.Cli -- papyrus validate "./Scripts/Source/MyMod_QuestScript.psc"

# 4. Compile to PEX
dotnet run --project src/SpookysAutomod.Cli -- papyrus compile "./Scripts/Source" --output "./Scripts" --headers "C:/Skyrim/Data/Scripts/Source"

# 5. Attach to quest in plugin
dotnet run --project src/SpookysAutomod.Cli -- esp attach-script "MyMod.esp" --quest "MyMod_MainQuest" --script "MyMod_QuestScript"
```

### Decompile and Analyze Existing Scripts
```bash
# 1. Decompile a single script
dotnet run --project src/SpookysAutomod.Cli -- papyrus decompile "./Scripts/SomeScript.pex" --output "./Decompiled"

# 2. Decompile all scripts in a directory
dotnet run --project src/SpookysAutomod.Cli -- papyrus decompile "./Scripts" --output "./Decompiled"

# 3. Read the decompiled source to understand behavior
```

### Create Magic Effect Script
```bash
# 1. Generate magic effect script template
dotnet run --project src/SpookysAutomod.Cli -- papyrus generate --name "MyMod_FireEffect" --extends ActiveMagicEffect --output "./Scripts/Source" --description "Fire damage over time effect"

# 2. Compile
dotnet run --project src/SpookysAutomod.Cli -- papyrus compile "./Scripts/Source/MyMod_FireEffect.psc" --output "./Scripts" --headers "C:/Skyrim/Data/Scripts/Source"
```

## Script Template Examples

### Quest Script (OnInit)
```papyrus
ScriptName MyMod_QuestScript extends Quest
{Main quest controller}

;-- Properties --
GlobalVariable Property MyMod_Enabled Auto

;-- Events --
Event OnInit()
    If MyMod_Enabled.GetValue() == 1
        Debug.Notification("My Mod initialized!")
    EndIf
EndEvent

;-- Functions --
Function DoSomething()
    ; Your code here
EndFunction
```

### Actor Script
```papyrus
ScriptName MyMod_ActorScript extends Actor
{Script attached to an actor}

Event OnLoad()
    Debug.Trace("Actor loaded: " + GetDisplayName())
EndEvent

Event OnDeath(Actor akKiller)
    Debug.Notification(GetDisplayName() + " was killed!")
EndEvent
```

### ActiveMagicEffect Script
```papyrus
ScriptName MyMod_EffectScript extends ActiveMagicEffect
{Script for a magic effect}

Event OnEffectStart(Actor akTarget, Actor akCaster)
    Debug.Notification("Effect started on " + akTarget.GetDisplayName())
EndEvent

Event OnEffectFinish(Actor akTarget, Actor akCaster)
    Debug.Notification("Effect ended")
EndEvent
```

## Troubleshooting Script Errors

### Common Compilation Errors

**"Missing header files":**
- Verify headers path points to vanilla Scripts/Source
- Run `papyrus status` to check tool availability

**"Script extends unknown type":**
- Ensure base type is spelled correctly
- Common types: Quest, Actor, ObjectReference, MagicEffect

**"Property not found":**
- Properties must be filled in the Creation Kit
- Use `GlobalVariable Property MyVar Auto` for globals

**"Function not found":**
- Check function exists in the parent type
- Papyrus is case-insensitive for identifiers

### Decompilation Issues

**"Failed to decompile":**
- Some heavily obfuscated scripts may not decompile
- Try newer Champollion versions

## Important Notes

1. **Scripts/Source vs Scripts** - Source (.psc) goes in Source folder, compiled (.pex) in Scripts
2. **Property linking** - Properties defined in scripts must be filled in Creation Kit
3. **Vanilla headers required** - Compilation needs vanilla script sources as imports
4. **Case insensitivity** - Papyrus identifiers are case-insensitive
5. **Use `--json` flag** for machine-readable output when scripting

## JSON Output

All commands support `--json` for structured output:
```bash
dotnet run --project src/SpookysAutomod.Cli -- papyrus status --json
```
