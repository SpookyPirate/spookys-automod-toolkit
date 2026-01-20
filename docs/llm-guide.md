# LLM Guide for Spooky's AutoMod Toolkit

**Version:** 1.5.0
**Purpose:** Comprehensive technical reference for workflow patterns and advanced features

---

## About This Guide

This is a **detailed technical reference** for LLMs working with the toolkit. It provides:
- Complete workflow patterns for common scenarios
- Advanced feature documentation (auto-fill, type inspection, dry-run)
- Detailed command examples with full syntax
- Troubleshooting and patching workflows
- Best practices for technical implementation

**📌 PREREQUISITES:** Read `llm-init-prompt.md` first for:
- Your role and behavioral guidelines
- Mandatory rules (ALWAYS/NEVER)
- Communication patterns and decision frameworks
- Setup checklist

This guide assumes you've already been initialized and focuses purely on technical how-to patterns.

---

## Quick Command Format

```bash
# All commands use this format from toolkit directory
dotnet run --project src/SpookysAutomod.Cli -- <module> <command> [args] [options]

# Always append --json for parseable output
dotnet run --project src/SpookysAutomod.Cli -- esp info "MyMod.esp" --json
```

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

### Pattern 2: Create Quest with Aliases and Scripts

Quest aliases are containers that can hold references to actors, objects, or locations. This pattern is used for follower systems, dynamic NPC tracking, and scripted quest objectives.

```bash
# 1. Create ESL-flagged plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "FollowerMod.esp" --light --author "LLM" --json

# 2. Add global configuration variables
dotnet run --project src/SpookysAutomod.Cli -- esp add-global "FollowerMod.esp" FM_Enabled --value 1 --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-global "FollowerMod.esp" FM_MaxFollowers --value 5 --json

# 3. Add factions for tracking
dotnet run --project src/SpookysAutomod.Cli -- esp add-faction "FollowerMod.esp" FM_TrackingFaction --name "Tracking Faction" --json

# 4. Add main quest with start-enabled flag
dotnet run --project src/SpookysAutomod.Cli -- esp add-quest "FollowerMod.esp" FM_MainQuest --name "Follower System" --start-enabled --run-once --json

# 5. Attach main script to quest
dotnet run --project src/SpookysAutomod.Cli -- esp attach-script "FollowerMod.esp" --quest FM_MainQuest --script FM_MainQuestScript --json

# 6. Add follower aliases with scripts attached
dotnet run --project src/SpookysAutomod.Cli -- esp add-alias "FollowerMod.esp" --quest FM_MainQuest --name FollowerAlias01 --script FM_FollowerAliasScript --flags "Optional,AllowReuseInQuest,AllowReserved" --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-alias "FollowerMod.esp" --quest FM_MainQuest --name FollowerAlias02 --script FM_FollowerAliasScript --flags "Optional,AllowReuseInQuest,AllowReserved" --json

# 7. Set script properties on quest script
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "FollowerMod.esp" --quest FM_MainQuest --script FM_MainQuestScript --property ModEnabled --value "FollowerMod.esp|0x800" --type object --json

# 8. Auto-fill properties from Skyrim.esm (RECOMMENDED for vanilla references)
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill "FollowerMod.esp" --quest FM_MainQuest --mod-folder "path/to/mod" --data-folder "path/to/Skyrim/Data" --json

# 9. Generate SEQ file
dotnet run --project src/SpookysAutomod.Cli -- esp generate-seq "FollowerMod.esp" --output "./" --json

# 10. Verify with analyze
dotnet run --project src/SpookysAutomod.Cli -- esp analyze "FollowerMod.esp" --json
```

**Understanding Quest Aliases:**

| Concept | Description |
|---------|-------------|
| **Quest Alias** | A slot in a quest that can hold a reference (actor, object, location) |
| **Alias Script** | Script attached to alias, monitors/affects whatever fills that alias |
| **Alias Flags** | `Optional` (doesn't need filling), `AllowReuseInQuest` (same ref can fill multiple), `AllowReserved` |
| **Fill Type** | How the alias gets filled - typically via script (`ForceRefTo()`) for dynamic systems |

**Common Alias Flags:**

- `Optional` - Alias doesn't need to be filled for quest to work
- `AllowReuseInQuest` - Same reference can fill multiple aliases
- `AllowReserved` - Allow reserved state
- `AllowDead` - Allow dead actors
- `Essential` - Make the alias reference essential
- `QuestObject` - Mark as quest object

Combine flags with commas: `--flags "Optional,AllowReuseInQuest,AllowReserved"`

### Pattern 3: Create Weapons and Armor Mod

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

### Pattern 4: Create Functional Spells

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

### Pattern 5: Create Functional Perks

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

### Pattern 6: Create Books and Lore (Works Immediately!)

Books are the simplest - no models or effects needed:

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "MyLoreMod" --light --author "LLM" --json

# Add books with custom text
dotnet run --project src/SpookysAutomod.Cli -- esp add-book "MyLoreMod.esp" "MLM_History" --name "The History of the Dragonborn" --text "In ages past, when dragons ruled the skies, there arose a hero..." --value 50 --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-book "MyLoreMod.esp" "MLM_Journal" --name "Adventurer's Journal" --text "Day 1: I set out from Whiterun today. The road was long..." --value 10 --json
```

### Pattern 7: Create Leveled Lists and Encounter Zones

Leveled lists distribute random loot, and encounter zones control level scaling:

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "LeveledContent.esp" --light --json

# Create a leveled item list for treasure chests
dotnet run --project src/SpookysAutomod.Cli -- esp add-leveled-item "LeveledContent.esp" "LC_TreasureChest" --chance-none 25 --preset low-treasure --json

# Or manually add entries (item,level,count)
dotnet run --project src/SpookysAutomod.Cli -- esp add-leveled-item "LeveledContent.esp" "LC_BossList" --chance-none 5 --add-entry "GoldBase,1,100" --add-entry "LockPick,5,3" --json

# Create encounter zones for dungeons
dotnet run --project src/SpookysAutomod.Cli -- esp add-encounter-zone "LeveledContent.esp" "LC_StarterDungeon" --preset low-level --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-encounter-zone "LeveledContent.esp" "LC_EndGameZone" --min-level 30 --max-level 50 --never-resets --json

# Create fully scaling zone (1-unlimited)
dotnet run --project src/SpookysAutomod.Cli -- esp add-encounter-zone "LeveledContent.esp" "LC_QuestZone" --preset scaling --json
```

**LeveledItem Presets:**
- `low-treasure` - 25% chance none, low-level items (starter areas)
- `medium-treasure` - 15% chance none, mid-level items (dungeons)
- `high-treasure` - 5% chance none, high-value items (boss chests)
- `guaranteed-loot` - 0% chance none, gives all items

**EncounterZone Presets:**
- `low-level` - Min 1, Max 10 (starter content)
- `mid-level` - Min 10, Max 30 (standard dungeons)
- `high-level` - Min 30, Max 50 (end-game content)
- `scaling` - Min 1, Max unlimited (quest content)

**Flags:**
- `--never-resets` - Enemies stay defeated
- `--disable-combat-boundary` - NPCs can pursue anywhere

### Pattern 8: Create Locations and Outfits

Locations define named areas for quests and fast travel. Outfits define NPC equipment sets:

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "WorldContent.esp" --light --json

# Create locations with presets
dotnet run --project src/SpookysAutomod.Cli -- esp add-location "WorldContent.esp" "WC_MyInn" --name "The Rusty Tankard" --preset inn --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-location "WorldContent.esp" "WC_PlayerHome" --name "Cozy Cottage" --preset dwelling --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-location "WorldContent.esp" "WC_MyDungeon" --name "Forgotten Crypt" --preset dungeon --json

# Add custom keywords
dotnet run --project src/SpookysAutomod.Cli -- esp add-location "WorldContent.esp" "WC_CustomLoc" --name "Special Place" --add-keyword "LocTypeCity" --json

# Set parent location
dotnet run --project src/SpookysAutomod.Cli -- esp add-location "WorldContent.esp" "WC_WhiterunShop" --name "My Shop" --parent-location "WhiterunHoldLocation" --json

# Create NPC outfits with presets
dotnet run --project src/SpookysAutomod.Cli -- esp add-outfit "WorldContent.esp" "WC_GuardOutfit" --preset guard --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-outfit "WorldContent.esp" "WC_FarmerOutfit" --preset farmer --json
dotnet run --project src/SpookysAutomod.Cli -- esp add-outfit "WorldContent.esp" "WC_MageOutfit" --preset mage --json

# Or add custom items
dotnet run --project src/SpookysAutomod.Cli -- esp add-outfit "WorldContent.esp" "WC_CustomOutfit" --add-item "ArmorIronCuirass" --add-item "WeaponIronSword" --json
```

**Location Presets:**
- `inn` - Adds LocTypeInn keyword (taverns, drinking establishments)
- `city` - Adds LocTypeCity keyword (major walled settlements)
- `dungeon` - Adds LocTypeDungeon keyword (caves, ruins)
- `dwelling` - Adds LocTypeDwelling keyword (player homes, NPC houses)

**Outfit Presets:**
- `guard` - Iron armor + sword + shield
- `farmer` - Basic clothing (farmer clothes, roughspun tunic)
- `mage` - Mage robes + hood
- `thief` - Leather armor set

### Pattern 9: Create Form Lists for Scripts

Form lists are collections of records used by scripts and conditions:

```bash
# Create plugin
dotnet run --project src/SpookysAutomod.Cli -- esp create "ScriptHelpers.esp" --light --json

# Add form list with vanilla references
dotnet run --project src/SpookysAutomod.Cli -- esp add-form-list "ScriptHelpers.esp" "SH_MetalKeywords" --add-form "Skyrim.esm:0x000896" --add-form "Skyrim.esm:0x000897" --json

# Add form list with mod records
dotnet run --project src/SpookysAutomod.Cli -- esp add-form-list "ScriptHelpers.esp" "SH_CustomWeapons" --add-form "WeaponEditorID1" --add-form "WeaponEditorID2" --json

# Use in scripts: FormList Property MyList Auto
# Then set property: --property MyList --value "ScriptHelpers.esp|0x800"
```

---

## Script Properties and Form References

### Understanding Script Properties

Script properties link Papyrus variables to game records. When you declare `GlobalVariable Property MyGlobal Auto` in a script, you must set that property in the ESP to point to an actual global record.

### Setting Script Properties

```bash
# Object property - links to a form (global, faction, keyword, quest, etc.)
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script ScriptName --property PropName --value "Plugin.esp|0xFormID" --type object

# Alias property - links to another alias in the same quest
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script ScriptName --property PropName --value "AliasName" --type alias

# Primitive types
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script ScriptName --property PropName --value "42" --type int
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script ScriptName --property PropName --value "3.14" --type float
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script ScriptName --property PropName --value "true" --type bool
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script ScriptName --property PropName --value "Hello" --type string
```

### Setting Properties on Alias Scripts

Use `--alias-target` to set properties on scripts attached to aliases:

```bash
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "Mod.esp" --quest QuestID --script AliasScriptName --alias-target AliasName --property PropName --value "Plugin.esp|0xFormID" --type object
```

### Form Key Format

Form keys use the format `Plugin.esp|0xFormID`:

```bash
# Reference a global in the same mod (FormID 0x800)
--value "MyMod.esp|0x800" --type object

# Reference a Skyrim.esm keyword
--value "Skyrim.esm|0x1CB87" --type object

# Reference a Skyrim.esm faction
--value "Skyrim.esm|0x28347" --type object
```

**Important:** Don't hardcode Skyrim.esm Form IDs! Use `esp auto-fill` instead - it searches by EditorID and ensures type correctness.

---

## Auto-Fill: Automatic Script Property Resolution

### What is Auto-Fill?

The `esp auto-fill` command automatically fills script properties by:
1. Reading property names and types from `.psc` (Papyrus source) files
2. Searching Skyrim.esm for records with matching EditorIDs
3. **Type-filtering**: Only matching records of the correct type

This prevents errors like matching a Location instead of a Keyword when both have similar names.

### Basic Usage

```bash
# Auto-fill a specific script (RECOMMENDED)
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill "MyMod.esp" \
  --quest MyQuest \
  --script MyQuestScript \
  --script-dir "./Scripts/Source" \
  --data-folder "path/to/Skyrim/Data"

# Target alias script
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill "MyMod.esp" \
  --quest MyQuest \
  --alias FollowerAlias01 \
  --script MyAliasScript \
  --script-dir "./Scripts/Source" \
  --data-folder "path/to/Skyrim/Data"

# Bulk auto-fill ALL scripts in mod (most efficient)
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill-all "MyMod.esp" \
  --script-dir "./Scripts/Source" \
  --data-folder "path/to/Skyrim/Data"

# Disable link cache (if you need fresh data)
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill-all "MyMod.esp" \
  --script-dir "./Scripts/Source" \
  --data-folder "path/to/Skyrim/Data" \
  --no-cache
```

### How Type-Aware Auto-Fill Works

When parsing a `.psc` file:
```papyrus
Keyword Property LocTypeInn Auto           ; Searches only IKeywordGetter
GlobalVariable Property ModEnabled Auto    ; Searches only IGlobalGetter
Quest Property MainQuest Auto              ; Searches only IQuestGetter
Faction Property TrackingFaction Auto      ; Searches only IFactionGetter
```

The auto-fill extracts the property type (`Keyword`, `GlobalVariable`, etc.) and only searches for records of that specific type. This prevents incorrect matches.

**Example Problem Solved:**

Without type filtering, searching for "LocTypeInn" might find:
- `LocTypeInn` (Keyword) at `0x01CB87` ← CORRECT
- `RiverwoodInn` (Location) at `0x01CB88` ← WRONG TYPE!

With type filtering, only the Keyword is matched.

### Supported Type Mappings

| Papyrus Type | Searches For |
|--------------|--------------|
| `Keyword` | Keywords only |
| `GlobalVariable` | Globals only |
| `Quest` | Quests only |
| `Faction` | Factions only |
| `Actor` / `ActorBase` | NPCs only |
| `Spell` | Spells only |
| `Perk` | Perks only |
| `Weapon` | Weapons only |
| `Armor` | Armor only |
| `Book` | Books only |
| `Location` | Locations only |
| `WorldSpace` | Worldspaces only |
| `MagicEffect` | Magic effects only |
| `FormList` | Form lists only |
| And 30+ more types... | |

### Array Property Support

Auto-fill now supports array properties using ScriptObjectListProperty:

```papyrus
Keyword[] Property AllKeywords Auto     ; Creates ScriptObjectListProperty
Location[] Property AllLocations Auto   ; Creates ScriptObjectListProperty with ExtendedList
```

**Current Behavior:** Array properties are correctly structured as ScriptObjectListProperty with the first matching FormKey. Each element is a ScriptObjectProperty with Alias=-1, Unused=0.

**Future Enhancement:** Pattern matching (e.g., "LocType*" → all LocTypeX keywords) and multi-element population from PSC comments or explicit commands.

### Performance: Link Cache Caching

The auto-fill system uses a cached link cache for significant performance improvements:

- **First run**: Loads Skyrim.esm, Update.esm, DLCs (~2-3 seconds)
- **Subsequent runs**: Uses cached link cache (~0.3 seconds)
- **Cache timeout**: 5 minutes
- **Force refresh**: Use `--no-cache` flag

**Best Practice:** Use `esp auto-fill-all` for bulk operations to maximize cache reuse across all scripts.

### When to Use Auto-Fill vs Manual Properties

| Scenario | Approach |
|----------|----------|
| Skyrim.esm keywords, worldspaces, factions | Use `esp auto-fill` |
| Properties in YOUR mod (globals, factions, quests) | Use `esp set-property` with specific FormID |
| Mixed (some yours, some vanilla) | Set yours first, then run auto-fill |

### Complete Auto-Fill Workflow

```bash
# 1. Create plugin and records
dotnet run --project src/SpookysAutomod.Cli -- esp create "MyMod.esp" --light
dotnet run --project src/SpookysAutomod.Cli -- esp add-global "MyMod.esp" MyMod_Enabled --value 1
dotnet run --project src/SpookysAutomod.Cli -- esp add-quest "MyMod.esp" MyMod_Quest --start-enabled
dotnet run --project src/SpookysAutomod.Cli -- esp attach-script "MyMod.esp" --quest MyMod_Quest --script MyQuestScript

# 2. Set mod-specific properties (FormIDs you control)
dotnet run --project src/SpookysAutomod.Cli -- esp set-property "MyMod.esp" \
  --quest MyMod_Quest --script MyQuestScript \
  --property ModEnabled --value "MyMod.esp|0x800" --type object

# 3. Auto-fill Skyrim.esm references (reads types from PSC)
# Option A: Single script
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill "MyMod.esp" \
  --quest MyMod_Quest \
  --script MyQuestScript \
  --script-dir "./Scripts/Source" \
  --data-folder "path/to/Skyrim/Data"

# Option B: All scripts at once (RECOMMENDED for multiple scripts)
dotnet run --project src/SpookysAutomod.Cli -- esp auto-fill-all "MyMod.esp" \
  --script-dir "./Scripts/Source" \
  --data-folder "path/to/Skyrim/Data"
```

---

## Dry-Run Mode and Debugging

### Preview Changes with Dry-Run

All `add-*` commands support `--dry-run` to preview changes without saving:

```bash
# Preview weapon creation
dotnet run --project src/SpookysAutomod.Cli -- esp add-weapon "MyMod.esp" "TestSword" \
  --name "Test Blade" --damage 30 --model iron-sword --dry-run

# Preview quest creation
dotnet run --project src/SpookysAutomod.Cli -- esp add-quest "MyMod.esp" "TestQuest" \
  --name "Test Quest" --start-enabled --dry-run

# Preview with JSON output
dotnet run --project src/SpookysAutomod.Cli -- esp add-spell "MyMod.esp" "TestSpell" \
  --name "Test Spell" --effect damage-health --magnitude 50 --dry-run --json
```

**Use Cases:**
- Validate FormIDs before committing
- Test command syntax
- Preview record structure
- Verify model paths and presets

### Debug Mutagen Types

Use `esp debug-types` to inspect Mutagen's type system:

```bash
# Show specific type
dotnet run --project src/SpookysAutomod.Cli -- esp debug-types "QuestAlias"

# Pattern matching
dotnet run --project src/SpookysAutomod.Cli -- esp debug-types "Quest*"

# Show all types (verbose)
dotnet run --project src/SpookysAutomod.Cli -- esp debug-types --all

# JSON output for parsing
dotnet run --project src/SpookysAutomod.Cli -- esp debug-types "QuestFragmentAlias" --json
```

**Output includes:**
- Property names and types
- Nullability and collection types
- Critical notes (e.g., "VirtualMachineAdapter NOT on QuestAlias!")

**Example Output:**
```
QuestAlias (Mutagen.Bethesda.Skyrim.QuestAlias)
  Type: Class
  Properties:
    ID: UInt16
    Name: String?
    Flags: QuestAlias.Flag
  Notes:
    - VirtualMachineAdapter NOT on QuestAlias!
    - Use QuestFragmentAlias in quest.VirtualMachineAdapter.Aliases instead
```

---

## Troubleshooting Existing Mods

### Inspect and Diagnose Issues

```bash
# 1. Check plugin contents and structure (basic info)
dotnet run --project src/SpookysAutomod.Cli -- esp info "BrokenMod.esp" --json

# 2. Detailed analysis including scripts, aliases, and properties
dotnet run --project src/SpookysAutomod.Cli -- esp analyze "BrokenMod.esp" --json

# 3. List master dependencies
dotnet run --project src/SpookysAutomod.Cli -- esp list-masters "BrokenMod.esp" --json

# 4. See what's in the BSA
dotnet run --project src/SpookysAutomod.Cli -- archive list "BrokenMod.bsa" --limit 0 --json

# 5. Check specific file types
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

# 5. Add new faction
dotnet run --project src/SpookysAutomod.Cli -- esp add-faction "ExistingMod.esp" "NewFaction" --name "My Faction" --json

# 6. Add new alias to existing quest
dotnet run --project src/SpookysAutomod.Cli -- esp add-alias "ExistingMod.esp" --quest "ExistingQuest" --name "NewAlias" --script "AliasScript" --json

# 7. Verify additions
dotnet run --project src/SpookysAutomod.Cli -- esp analyze "ExistingMod.esp" --json
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

### Create and Build Native Plugin

**Requirements:** User must have CMake and MSVC Build Tools installed (see README "SKSE C++ Build Tools" section).

```bash
# 1. Check if build tools are available
cmake --version
cl  # Should show MSVC compiler version

# 2. Create project with Papyrus native function support
dotnet run --project src/SpookysAutomod.Cli -- skse create "MyNativePlugin" --template papyrus-native --author "LLM" --output "./" --json

# 3. Add custom functions
dotnet run --project src/SpookysAutomod.Cli -- skse add-function "./MyNativePlugin" --name "GetActorSpeed" --return "Float" --param "Actor:target" --json
dotnet run --project src/SpookysAutomod.Cli -- skse add-function "./MyNativePlugin" --name "SetActorSpeed" --return "void" --param "Actor:target" --param "Float:speed" --json

# 4. Build with CMake (no Visual Studio IDE needed)
cd MyNativePlugin
cmake -B build -S .
cmake --build build --config Release

# 5. Output DLL ready to use
# Result: build/Release/MyNativePlugin.dll
# Install to: Data/SKSE/Plugins/MyNativePlugin.dll
```

**If build tools missing:**
- Guide user to install CMake: https://cmake.org/download/
- Guide user to install MSVC Build Tools: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022
- See README "SKSE C++ Build Tools" section for detailed setup

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
| Detailed analysis | `esp analyze plugin` |
| Add quest | `esp add-quest plugin editorId [--name] [--start-enabled] [--run-once]` |
| Add spell | `esp add-spell plugin editorId [--name] [--effect] [--magnitude]` |
| Add perk | `esp add-perk plugin editorId [--name] [--effect] [--bonus] [--playable]` |
| Add global | `esp add-global plugin editorId [--type] [--value]` |
| Add faction | `esp add-faction plugin editorId [--name] [--hidden] [--track-crime]` |
| Add weapon | `esp add-weapon plugin editorId [--name] [--type] [--damage] [--model]` |
| Add armor | `esp add-armor plugin editorId [--name] [--type] [--slot] [--model]` |
| Add NPC | `esp add-npc plugin editorId [--name] [--level] [--essential]` |
| Add book | `esp add-book plugin editorId [--name] [--text]` |
| Add leveled item | `esp add-leveled-item plugin editorId [--chance-none] [--add-entry] [--preset]` |
| Add form list | `esp add-form-list plugin editorId [--add-form]` |
| Add encounter zone | `esp add-encounter-zone plugin editorId [--min-level] [--max-level] [--preset]` |
| Add location | `esp add-location plugin editorId [--name] [--parent-location] [--preset]` |
| Add outfit | `esp add-outfit plugin editorId [--add-item] [--preset]` |
| Add alias | `esp add-alias plugin --quest id --name name [--script] [--flags]` |
| Attach script | `esp attach-script plugin --quest id --script name` |
| Attach alias script | `esp attach-alias-script plugin --quest id --alias name --script name` |
| Set property | `esp set-property plugin --quest id --script name --property name --value val --type type [--alias-target]` |
| **Auto-fill** | `esp auto-fill plugin --quest id [--alias name] [--mod-folder path] [--properties "A,B"] --data-folder path` |
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

## Technical Best Practices

### Naming Conventions

**EditorIDs:**
- Use unique prefixes: `MyMod_` or `MM_`
- Scripts: `MyMod_QuestScript`, `MyMod_FollowerScript`
- Globals: `bMyMod_Enabled`, `fMyMod_Multiplier`, `iMyMod_Count`
- Quests: `MyMod_MainQuest`, `MyMod_TrackerQuest`
- Aliases: `FollowerAlias01`, `EnemyAlias`, `LocationAlias`

**Global Prefixes by Type:**
- Bool: `bMyMod_Feature`
- Float: `fMyMod_Multiplier`
- Int: `iMyMod_Counter`

### File Organization

```
MyMod/
  MyMod.esp              # Plugin file
  MyMod.seq              # SEQ file (if start-enabled quests)
  Scripts/
    Source/              # PSC source files
      MyMod_QuestScript.psc
      MyMod_AliasScript.psc
    *.pex                # Compiled scripts
  MCM/
    config/
      MyMod/
        config.json      # MCM Helper configuration
  Sound/
    Voice/
      MyMod.esp/
        NPCName/
          *.fuz          # Voice files
```

### ESL Form ID Limits

When creating ESL-flagged plugins (`--light`):
- Form IDs are automatically assigned starting from 0x800
- Maximum of 4096 records (0x800 - 0xFFF range)
- If you exceed this, the plugin will fail to load

### What Works Immediately vs Needs Additional Assets

| Type | Status | Notes |
|------|--------|-------|
| Books | Works immediately | No dependencies |
| Quests | Works immediately | Need scripts for logic |
| Globals | Works immediately | For configuration |
| Factions | Works immediately | For tracking/relations |
| Aliases | Works immediately | Need scripts for behavior |
| Spells | Needs `--effect` | Without effect, spell does nothing |
| Perks | Needs `--effect` | Without effect, perk does nothing |
| Weapons | Needs `--model` | Without model, invisible |
| Armor | Needs `--model` | Without model, invisible |
| NPCs | Record only | Need race/face data for visibility |

---

## Technical Architecture Notes

### Quest Alias Scripts in Mutagen

**Critical Understanding:** Alias scripts are NOT stored on the `QuestAlias` object itself. They're stored in `QuestFragmentAlias` within the quest's `VirtualMachineAdapter`:

```
Quest
 └── VirtualMachineAdapter (QuestAdapter)
      ├── Scripts[]           ← Quest scripts
      └── Aliases[]           ← QuestFragmentAlias objects
           ├── Property       ← Links to alias by index, MUST include Object = quest FormKey
           └── Scripts[]      ← Alias scripts
```

The toolkit handles this automatically when you use:
- `esp add-alias --script ScriptName`
- `esp attach-alias-script`
- `esp set-property --alias-target`

**Important:** `QuestFragmentAlias.Property.Object` must reference the owning quest's FormKey for the Creation Kit to recognize the alias scripts. The toolkit sets this automatically.

### Fill Types for Aliases

| Fill Type | Use Case |
|-----------|----------|
| **Specific Reference** | Script-managed aliases (use `ForceRefTo()` in Papyrus) |
| **Find Matching Reference** | Auto-fill based on conditions (needs Match Conditions) |
| **Unique Actor** | Fill with a specific unique NPC |
| **Create Reference to Object** | Create new reference at runtime |

For dynamic systems (follower tracking, etc.), use **Specific Reference** and manage via script.
