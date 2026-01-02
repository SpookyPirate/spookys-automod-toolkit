# LLM Guide for Spooky's AutoMod Toolkit

This guide is specifically designed for LLMs to understand how to use the toolkit effectively for autonomous Skyrim mod creation, troubleshooting, editing, and patching.

## Quick Reference

```bash
# All commands start from toolkit directory
cd "C:\Users\spook\Desktop\Projects\3. Development\skyrim-mods\spookys-automod-toolkit"

# All commands use this format
dotnet run --project src/SpookysAutomod.Cli -- <module> <command> [args] [options]
```

## Core Principles

1. **Always use `--json` flag** for parsing command output
2. **Check success status** before proceeding with dependent operations
3. **Use suggestions** from error responses to fix issues
4. **Chain commands** to build complete mods
5. **Use unique prefixes** for Editor IDs (e.g., `MyMod_`)
6. **Papyrus compilation requires script headers** - See README section "Papyrus Script Headers" for setup

---

## Creating New Mods

### Pattern 1: Create a Quest Mod with Script

```bash
# 1. Create the plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "MyQuestMod" --light --author "LLM" --json

# 2. Add a main quest
dotnet run --project src/SpookysAutomod.Cli -- esp add-quest "MyQuestMod.esp" "MQM_MainQuest" --name "The Main Quest" --start-enabled --json

# 3. Add a configuration global
dotnet run --project src/SpookysAutomod.Cli -- esp add-global "MyQuestMod.esp" "MQM_Enabled" --type long --value 1 --json

# 4. Generate the quest script
dotnet run --project src/SpookysAutomod.Cli -- papyrus generate --name "MQM_MainQuestScript" --extends "Quest" --output "./Scripts/Source" --json

# 5. Edit the generated script to add your logic

# 6. Compile the script (requires script headers - see README)
dotnet run --project src/SpookysAutomod.Cli -- papyrus compile "./Scripts/Source" --output "./Scripts" --headers "./skyrim-script-headers" --json

# 7. Attach script to quest
dotnet run --project src/SpookysAutomod.Cli -- esp attach-script "MyQuestMod.esp" --quest "MQM_MainQuest" --script "MQM_MainQuestScript" --json

# 8. Generate SEQ file for start-enabled quest
dotnet run --project src/SpookysAutomod.Cli -- esp generate-seq "MyQuestMod.esp" --output "./" --json

# 9. Create MCM configuration
dotnet run --project src/SpookysAutomod.Cli -- mcm create "MyQuestMod" "My Quest Mod" --output "./MCM/config/MyQuestMod/config.json" --json
dotnet run --project src/SpookysAutomod.Cli -- mcm add-toggle "./MCM/config/MyQuestMod/config.json" "bEnabled" "Enable Mod" --help-text "Toggle the mod on/off" --json
```

### Pattern 2: Create Weapons and Armor Mod

**CRITICAL**: Weapons and armor REQUIRE `--model` to be visible in-game!

```bash
# 1. Create the plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "MyGearMod" --light --author "LLM" --json

# 2. Add weapons with model presets
dotnet run --project src/SpookysAutomod.Cli -- esp add-weapon "MyGearMod.esp" "MGM_Sword" --name "Blade of Power" --type sword --damage 35 --value 500 --model iron-sword --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-weapon "MyGearMod.esp" "MGM_Bow" --name "Hunter's Revenge" --type bow --damage 25 --model hunting-bow --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-weapon "MyGearMod.esp" "MGM_Dagger" --name "Shadow's Edge" --type dagger --damage 15 --model iron-dagger --json

# 3. Add armor set with model presets
dotnet run --project src/SpookysAutomod.Cli -- esp add-armor "MyGearMod.esp" "MGM_Cuirass" --name "Power Cuirass" --type heavy --slot body --rating 50 --model iron-cuirass --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-armor "MyGearMod.esp" "MGM_Helmet" --name "Power Helmet" --type heavy --slot head --rating 25 --model iron-helmet --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-armor "MyGearMod.esp" "MGM_Gauntlets" --name "Power Gauntlets" --type heavy --slot hands --rating 18 --model iron-gauntlets --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-armor "MyGearMod.esp" "MGM_Boots" --name "Power Boots" --type heavy --slot feet --rating 18 --model iron-boots --json

# 4. Verify
dotnet run --project src/SpookysAutomod.Cli -- esp info "MyGearMod.esp" --json
```

**Model Presets:**
- Weapons: `iron-sword`, `steel-sword`, `iron-dagger`, `hunting-bow`
- Armor: `iron-cuirass`, `iron-helmet`, `iron-gauntlets`, `iron-boots`, `iron-shield`
- Custom: `--model "Weapons\Daedric\DaedricSword.nif"`

### Pattern 3: Create Functional Spells

**CRITICAL**: Spells REQUIRE `--effect` to actually do something!

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "SpellPack.esp" --light --json

# Damage spell - 75 damage
dotnet run --project src/SpookysAutomod.Cli -- esp add-spell "SpellPack.esp" "SP_Fireball" --name "Greater Fireball" --effect damage-health --magnitude 75 --cost 60 --json

# Healing spell - restore 100 health
dotnet run --project src/SpookysAutomod.Cli -- esp add-spell "SpellPack.esp" "SP_Heal" --name "Major Healing" --effect restore-health --magnitude 100 --cost 50 --json

# Buff spell - +50 health for 120 seconds
dotnet run --project src/SpookysAutomod.Cli -- esp add-spell "SpellPack.esp" "SP_Fortify" --name "Warrior's Blessing" --effect fortify-health --magnitude 50 --duration 120 --cost 80 --json

# Power (once daily) - restore all magicka
dotnet run --project src/SpookysAutomod.Cli -- esp add-spell "SpellPack.esp" "SP_RacialPower" --name "Ancient Power" --type power --effect restore-magicka --magnitude 500 --json
```

**Spell Effect Presets:**
- `damage-health`, `restore-health`
- `damage-magicka`, `restore-magicka`
- `damage-stamina`, `restore-stamina`
- `fortify-health`, `fortify-magicka`, `fortify-stamina`
- `fortify-armor`, `fortify-attack`

### Pattern 4: Create Functional Perks

**CRITICAL**: Perks REQUIRE `--effect` to actually do something!

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "PerkMod.esp" --light --json

# Combat perk - +25% weapon damage
dotnet run --project src/SpookysAutomod.Cli -- esp add-perk "PerkMod.esp" "PM_WeaponMaster" --name "Weapon Master" --description "+25% weapon damage" --effect weapon-damage --bonus 25 --playable --json

# Defense perk - 15% damage reduction
dotnet run --project src/SpookysAutomod.Cli -- esp add-perk "PerkMod.esp" "PM_ThickSkin" --name "Thick Skin" --description "Take 15% less damage" --effect damage-reduction --bonus 15 --playable --json

# Magic perk - 20% spell cost reduction
dotnet run --project src/SpookysAutomod.Cli -- esp add-perk "PerkMod.esp" "PM_Efficiency" --name "Magical Efficiency" --description "Spells cost 20% less" --effect spell-cost --bonus 20 --playable --json

# Stealth perk - 2x sneak attack damage
dotnet run --project src/SpookysAutomod.Cli -- esp add-perk "PerkMod.esp" "PM_Assassin" --name "Assassin's Strike" --description "2x sneak attack damage" --effect sneak-attack --bonus 100 --playable --json
```

**Perk Effect Presets:**
- `weapon-damage` - Increase weapon damage by X%
- `damage-reduction` - Reduce incoming damage by X%
- `armor` - Increase armor rating by X%
- `spell-cost` - Reduce spell cost by X%
- `spell-power` - Increase spell magnitude by X%
- `spell-duration` - Increase spell duration by X%
- `sneak-attack` - Increase sneak attack multiplier
- `pickpocket` - Increase pickpocket chance by X%
- `prices` - Improve buying/selling prices by X%

### Pattern 5: Create Books and Lore (Works Immediately!)

Books are the simplest - no models or effects needed:

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "MyLoreMod" --light --author "LLM" --json

# Add books with custom text
dotnet run --project src/SpookysAutomod.Cli -- esp add-book "MyLoreMod.esp" "MLM_History" --name "The History of the Dragonborn" --text "In ages past, when dragons ruled the skies, there arose a hero..." --value 50 --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-book "MyLoreMod.esp" "MLM_Journal" --name "Adventurer's Journal" --text "Day 1: I set out from Whiterun today. The road was long..." --value 10 --json
```

---

## Troubleshooting Existing Mods

### Inspect and Diagnose Issues

```bash
# 1. Check plugin contents and structure
dotnet run --project src/SpookysAutomod.Cli -- esp info "BrokenMod.esp" --json

# 2. List master dependencies
dotnet run --project src/SpookysAutomod.Cli -- esp list-masters "BrokenMod.esp" --json

# 3. See what's in the BSA
dotnet run --project src/SpookysAutomod.Cli -- archive list "BrokenMod.bsa" --limit 0 --json

# 4. Check specific file types
dotnet run --project src/SpookysAutomod.Cli -- archive list "BrokenMod.bsa" --filter "*.nif" --json
dotnet run --project src/SpookysAutomod.Cli -- archive list "BrokenMod.bsa" --filter "*.pex" --json
```

### Extract and Analyze

```bash
# Extract entire BSA
dotnet run --project src/SpookysAutomod.Cli -- archive extract "BrokenMod.bsa" --output "./Debug" --json

# Extract only scripts
dotnet run --project src/SpookysAutomod.Cli -- archive extract "BrokenMod.bsa" --output "./Debug" --filter "scripts/*" --json

# Decompile scripts to understand behavior
dotnet run --project src/SpookysAutomod.Cli -- papyrus decompile "./Debug/scripts" --output "./Debug/Source" --json

# Check mesh textures (find missing texture issues)
dotnet run --project src/SpookysAutomod.Cli -- nif textures "./Debug/meshes/SomeArmor.nif" --json

# Check NIF file info
dotnet run --project src/SpookysAutomod.Cli -- nif info "./Debug/meshes/SomeArmor.nif" --json
```

### Common Issues and Solutions

**Purple/Missing Textures:**
```bash
# 1. Check what textures the mesh expects
dotnet run --project src/SpookysAutomod.Cli -- nif textures "./Meshes/item.nif"

# 2. Verify those textures exist
# If missing, that's the problem
```

**Invisible Items:**
```bash
# 1. Check if mesh is valid
dotnet run --project src/SpookysAutomod.Cli -- nif info "./Meshes/item.nif"

# 2. If valid, check ESP model path matches actual file location
dotnet run --project src/SpookysAutomod.Cli -- esp info "Mod.esp" --json
```

**Script Errors:**
```bash
# 1. Decompile to see the source
dotnet run --project src/SpookysAutomod.Cli -- papyrus decompile "./Scripts/BrokenScript.pex" --output "./Debug"

# 2. Check syntax
dotnet run --project src/SpookysAutomod.Cli -- papyrus validate "./Debug/BrokenScript.psc"

# 3. Fix and recompile
dotnet run --project src/SpookysAutomod.Cli -- papyrus compile "./Debug/BrokenScript.psc" --output "./Scripts" --headers "./skyrim-script-headers"
```

---

## Editing Existing Mods

### Add Content to Existing Plugin

```bash
# 1. Inspect current contents
dotnet run --project src/SpookysAutomod.Cli -- esp info "ExistingMod.esp" --json

# 2. Add new weapon
dotnet run --project src/SpookysAutomod.Cli -- esp add-weapon "ExistingMod.esp" "NewSword" --name "Bonus Sword" --damage 35 --model iron-sword --json

# 3. Add new spell
dotnet run --project src/SpookysAutomod.Cli -- esp add-spell "ExistingMod.esp" "NewSpell" --name "Bonus Spell" --effect damage-health --magnitude 50 --cost 40 --json

# 4. Add new perk
dotnet run --project src/SpookysAutomod.Cli -- esp add-perk "ExistingMod.esp" "NewPerk" --name "Bonus Perk" --effect weapon-damage --bonus 10 --playable --json

# 5. Verify additions
dotnet run --project src/SpookysAutomod.Cli -- esp info "ExistingMod.esp" --json
```

### Scale Meshes

```bash
# Make weapon 50% larger
dotnet run --project src/SpookysAutomod.Cli -- nif scale "./Meshes/weapon.nif" 1.5 --output "./Meshes/weapon_large.nif"

# Make armor 25% smaller
dotnet run --project src/SpookysAutomod.Cli -- nif scale "./Meshes/armor.nif" 0.75 --output "./Meshes/armor_small.nif"
```

### Modify MCM Settings

```bash
# Check existing MCM
dotnet run --project src/SpookysAutomod.Cli -- mcm info "./MCM/config.json" --json

# Add new toggle
dotnet run --project src/SpookysAutomod.Cli -- mcm add-toggle "./MCM/config.json" "bNewFeature" "Enable New Feature"

# Add new slider
dotnet run --project src/SpookysAutomod.Cli -- mcm add-slider "./MCM/config.json" "fNewValue" "New Multiplier" --min 0.5 --max 2.0 --step 0.1

# Validate
dotnet run --project src/SpookysAutomod.Cli -- mcm validate "./MCM/config.json"
```

---

## Creating Patches

### Merge Records from Multiple Mods

```bash
# Merge patch records into main mod
dotnet run --project src/SpookysAutomod.Cli -- esp merge "Patch.esp" "MainMod.esp" --output "MainMod_Patched.esp" --json
```

### Create Compatibility Patch

```bash
# 1. Create new patch plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "ModA_ModB_Patch.esp" --light --json

# 2. Add overriding records
# (Records in the patch will override records with same EditorID in masters)
dotnet run --project src/SpookysAutomod.Cli -- esp add-weapon "ModA_ModB_Patch.esp" "SharedWeapon" --name "Balanced Sword" --damage 30 --model iron-sword --json
```

---

## Audio Workflows

### Extract Voice Files for Analysis

```bash
# Extract from BSA
dotnet run --project src/SpookysAutomod.Cli -- archive extract "VoiceMod.bsa" --output "./Extracted" --filter "sound/*"

# Extract FUZ to components
dotnet run --project src/SpookysAutomod.Cli -- audio extract-fuz "./Extracted/Sound/Voice/Mod.esp/NPC/Line.fuz" --output "./Audio"
```

### Create New Voice Line

```bash
# 1. Convert WAV to XWM
dotnet run --project src/SpookysAutomod.Cli -- audio wav-to-xwm "./Source/line.wav" --output "./Audio/line.xwm"

# 2. Create FUZ (with lip sync if available)
dotnet run --project src/SpookysAutomod.Cli -- audio create-fuz "./Audio/line.xwm" --lip "./Audio/line.lip" --output "./Sound/Voice/MyMod.esp/NPC/line.fuz"

# Or without lip sync
dotnet run --project src/SpookysAutomod.Cli -- audio create-fuz "./Audio/line.xwm" --output "./Sound/Voice/MyMod.esp/NPC/line.fuz"
```

---

## SKSE Plugin Development

### Create Native Plugin

```bash
# Create project with Papyrus native function support
dotnet run --project src/SpookysAutomod.Cli -- skse create "MyNativePlugin" --template papyrus-native --author "LLM" --output "./"

# Add custom functions
dotnet run --project src/SpookysAutomod.Cli -- skse add-function "./MyNativePlugin" --name "GetActorSpeed" --return "Float" --param "Actor:target"
dotnet run --project src/SpookysAutomod.Cli -- skse add-function "./MyNativePlugin" --name "SetActorSpeed" --return "void" --param "Actor:target" --param "Float:speed"

# Build (requires Visual Studio and CMake)
# cd MyNativePlugin
# cmake -B build -S .
# cmake --build build --config Release
```

---

## JSON Response Handling

### Success Response
```json
{
  "success": true,
  "result": {
    // Command-specific data
  }
}
```

### Error Response
```json
{
  "success": false,
  "error": "Error message",
  "errorContext": "Additional context about the error",
  "suggestions": [
    "Suggested fix 1",
    "Suggested fix 2"
  ]
}
```

### Handling Errors

When success is false:
1. Read the error message to understand the problem
2. Check errorContext for specific details
3. Apply one of the suggestions to fix the issue
4. Retry the command

---

## Command Reference by Module

### ESP (Plugin Files)
| Task | Command |
|------|---------|
| Create plugin | `esp create name [--light] [--author]` |
| Get info | `esp info plugin` |
| Add quest | `esp add-quest plugin editorId [--name] [--start-enabled]` |
| Add spell | `esp add-spell plugin editorId [--name] [--effect] [--magnitude]` |
| Add perk | `esp add-perk plugin editorId [--name] [--effect] [--bonus] [--playable]` |
| Add global | `esp add-global plugin editorId [--type] [--value]` |
| Add weapon | `esp add-weapon plugin editorId [--name] [--type] [--damage] [--model]` |
| Add armor | `esp add-armor plugin editorId [--name] [--type] [--slot] [--model]` |
| Add NPC | `esp add-npc plugin editorId [--name] [--level] [--essential]` |
| Add book | `esp add-book plugin editorId [--name] [--text]` |
| Attach script | `esp attach-script plugin --quest id --script name` |
| Generate SEQ | `esp generate-seq plugin --output dir` |
| List masters | `esp list-masters plugin` |
| Merge plugins | `esp merge source target [--output]` |

### Papyrus (Scripts)
| Task | Command |
|------|---------|
| Check tools | `papyrus status` |
| Download tools | `papyrus download` |
| Generate template | `papyrus generate --name name --extends type --output dir` |
| Compile | `papyrus compile source --output dir --headers dir` |
| Decompile | `papyrus decompile pex --output dir` |
| Validate | `papyrus validate psc` |

### Archive (BSA/BA2)
| Task | Command |
|------|---------|
| Check tools | `archive status` |
| Get info | `archive info archive` |
| List contents | `archive list archive [--filter] [--limit]` |
| Extract | `archive extract archive --output dir` |
| Create | `archive create directory --output file [--compress] [--game]` |

### MCM (Configuration)
| Task | Command |
|------|---------|
| Create config | `mcm create modName displayName --output file` |
| Get info | `mcm info config` |
| Validate | `mcm validate config` |
| Add toggle | `mcm add-toggle config id text [--help-text]` |
| Add slider | `mcm add-slider config id text --min --max [--step]` |

### NIF (Meshes)
| Task | Command |
|------|---------|
| Get info | `nif info file` |
| List textures | `nif textures file` |
| Scale mesh | `nif scale file factor [--output]` |
| Copy | `nif copy file --output file` |

### Audio
| Task | Command |
|------|---------|
| Get info | `audio info file` |
| Extract FUZ | `audio extract-fuz fuz --output dir` |
| Create FUZ | `audio create-fuz xwm --output file [--lip]` |
| WAV to XWM | `audio wav-to-xwm wav --output file` |

### SKSE (C++ Plugins)
| Task | Command |
|------|---------|
| List templates | `skse templates` |
| Create project | `skse create name --template template [--author] [--output]` |
| Get info | `skse info project` |
| Add function | `skse add-function project --name func [--return] [--param]` |

---

## Best Practices

### Naming Conventions
- Use unique prefixes for EditorIDs: `MyMod_` or `MM_`
- Scripts match pattern: `MyMod_QuestScript`
- Globals follow conventions: `bMyMod_Enabled`, `fMyMod_Multiplier`

### File Organization
```
MyMod/
  MyMod.esp              # Plugin file
  MyMod.seq              # SEQ file (if start-enabled quests)
  Scripts/
    Source/              # PSC source files
    *.pex                # Compiled scripts
  MCM/
    config/
      MyMod/
        config.json
  Sound/
    Voice/
      MyMod.esp/
        NPCName/
          *.fuz
```

### Error Prevention
1. Check tool status before operations: `papyrus status`, `archive status`
2. Validate before compiling: `papyrus validate`, `mcm validate`
3. Get info before modifications: `esp info`, `nif info`
4. Use `--json` for all operations when scripting

### What Works Immediately vs Needs Additional Assets

| Type | Status | Notes |
|------|--------|-------|
| Books | Works immediately | No dependencies |
| Quests | Works immediately | Need scripts for logic |
| Globals | Works immediately | For configuration |
| Spells | Needs `--effect` | Without effect, spell does nothing |
| Perks | Needs `--effect` | Without effect, perk does nothing |
| Weapons | Needs `--model` | Without model, invisible |
| Armor | Needs `--model` | Without model, invisible |
| NPCs | Record only | Need race/face data for visibility |
