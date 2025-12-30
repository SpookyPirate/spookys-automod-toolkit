# Spooky's AutoMod Toolkit Documentation

Complete reference documentation for all modules.

## Module References

| Module | Purpose | Documentation |
|--------|---------|---------------|
| ESP | Plugin files (.esp/.esl) | [esp.md](esp.md) |
| Papyrus | Script compilation/decompilation | [papyrus.md](papyrus.md) |
| Archive | BSA/BA2 archives | [archive.md](archive.md) |
| MCM | Mod configuration menus | [mcm.md](mcm.md) |
| NIF | 3D mesh files | [nif.md](nif.md) |
| Audio | FUZ/XWM/WAV audio | [audio.md](audio.md) |
| SKSE | SKSE C++ plugin projects | [skse.md](skse.md) |

## Quick Links

- [LLM Usage Guide](llm-guide.md) - Patterns and examples for AI assistants
- [Main README](../README.md) - Installation and quick start

---

## Overview

This toolkit enables LLMs and developers to create and edit Skyrim mods programmatically:

- **ESP/ESL Plugins** - Create and modify plugin files with quests, spells, globals, weapons, armor, NPCs, books, perks
- **Papyrus Scripts** - Compile, decompile, validate, and generate Papyrus scripts
- **NIF Meshes** - Inspect mesh files, extract textures, scale models
- **BSA/BA2 Archives** - Create and extract game archives
- **MCM Configuration** - Generate MCM Helper JSON configurations
- **Audio Files** - Convert and manipulate FUZ, XWM, and WAV audio
- **SKSE Plugins** - Generate C++ SKSE plugin projects with CommonLibSSE-NG

---

## Global Options

All commands support these options:

| Option | Description |
|--------|-------------|
| `--json` | Output in JSON format for machine parsing |
| `--verbose` | Show detailed output |
| `--help` | Show command help |

---

## Command Structure

```
spookys-automod <module> <command> [arguments] [options]
```

Examples:
```bash
spookys-automod esp create "MyMod.esp" --light
spookys-automod papyrus compile "./Scripts" --output "./Scripts" --headers "..."
spookys-automod archive extract "MyMod.bsa" --output "./Extracted"
```

---

## JSON Output Format

All commands return consistent JSON when using `--json`:

**Success:**
```json
{
  "success": true,
  "result": { ... }
}
```

**Error:**
```json
{
  "success": false,
  "error": "Error message",
  "errorContext": "Additional details",
  "suggestions": ["Suggested fix 1", "Suggested fix 2"]
}
```

---

## External Tools

| Tool | Module | Auto-Download | Manual Install |
|------|--------|---------------|----------------|
| papyrus-compiler | Papyrus | Yes | - |
| Champollion | Papyrus | Yes | - |
| BSArch | Archive | No | [xEdit releases](https://github.com/TES5Edit/TES5Edit/releases) |

---

## Common Workflows

### Create a Complete Mod

```bash
# 1. Create plugin
esp create "MyMod.esp" --light --author "YourName"

# 2. Add content
esp add-book "MyMod.esp" "MyBook" --name "Lore Book" --text "Content..."
esp add-weapon "MyMod.esp" "MySword" --name "Epic Sword" --damage 30 --model iron-sword

# 3. Check results
esp info "MyMod.esp"
```

### Create a Quest with Script

```bash
# 1. Create plugin and quest
esp create "MyQuestMod.esp" --light
esp add-quest "MyQuestMod.esp" "MyQuest" --name "My Quest" --start-enabled

# 2. Generate and compile script
papyrus generate --name "MyQuestScript" --extends Quest --output "./Scripts/Source"
papyrus compile "./Scripts/Source" --output "./Scripts" --headers "C:/Skyrim/Data/Scripts/Source"

# 3. Attach script and generate SEQ
esp attach-script "MyQuestMod.esp" --quest "MyQuest" --script "MyQuestScript"
esp generate-seq "MyQuestMod.esp" --output "./"
```

### Create MCM Configuration

```bash
# 1. Create config
mcm create "MyMod" "My Mod Settings" --output "./MCM/config.json"

# 2. Add controls
mcm add-toggle "./MCM/config.json" "bEnabled" "Enable Mod"
mcm add-slider "./MCM/config.json" "fDamage" "Damage Multiplier" --min 0.5 --max 2.0

# 3. Validate
mcm validate "./MCM/config.json"
```

### Package Mod as BSA

```bash
# 1. Organize files in Data structure
# MyModData/meshes/..., MyModData/textures/..., etc.

# 2. Create archive
archive create "./MyModData" --output "MyMod.bsa" --compress
```

---

## Quick Command Reference

### ESP
```bash
esp create <name> [--light] [--author]
esp info <plugin>
esp add-quest <plugin> <editorId> [--name] [--start-enabled]
esp add-spell <plugin> <editorId> [--name] [--type] [--cost] [--effect] [--magnitude] [--duration]
esp add-global <plugin> <editorId> [--type] [--value]
esp add-weapon <plugin> <editorId> [--name] [--type] [--damage] [--model]
esp add-armor <plugin> <editorId> [--name] [--type] [--slot] [--rating] [--model]
esp add-npc <plugin> <editorId> [--name] [--level] [--essential]
esp add-book <plugin> <editorId> [--name] [--text] [--value]
esp add-perk <plugin> <editorId> [--name] [--description] [--playable] [--effect] [--bonus]
esp attach-script <plugin> --quest <id> --script <name>
esp generate-seq <plugin> --output <dir>
esp list-masters <plugin>
esp merge <source> <target> [--output]
```

### Papyrus
```bash
papyrus status
papyrus download
papyrus compile <source> --output <dir> --headers <dir>
papyrus decompile <pex> --output <dir>
papyrus validate <psc>
papyrus generate --name <name> --extends <type> --output <dir>
```

### Archive
```bash
archive info <archive>
archive list <archive> [--filter] [--limit]
archive extract <archive> --output <dir>
archive create <directory> --output <file> [--compress] [--game]
archive status
```

### MCM
```bash
mcm create <modName> <displayName> --output <file>
mcm info <config>
mcm validate <config>
mcm add-toggle <config> <id> <text> [--help-text]
mcm add-slider <config> <id> <text> --min --max [--step]
```

### NIF
```bash
nif info <file>
nif textures <file>
nif scale <file> <factor> [--output]
nif copy <file> --output <file>
```

### Audio
```bash
audio info <file>
audio extract-fuz <fuz> --output <dir>
audio create-fuz <xwm> [--lip] --output <file>
audio wav-to-xwm <wav> --output <file>
```

### SKSE
```bash
skse templates
skse create <name> [--template] [--author] [--output]
skse info <project>
skse add-function <project> --name <name> [--return] [--param]
```
