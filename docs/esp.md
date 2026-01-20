# ESP Module Reference

The ESP module handles creation and editing of Skyrim plugin files (.esp/.esl).

## Commands

### create

Create a new ESP/ESL plugin file.

```bash
esp create <name> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `name` | Plugin filename (e.g., "MyMod.esp") |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--output`, `-o` | `.` | Output directory |
| `--light` | false | Create as ESL-flagged light plugin |
| `--author` | - | Author name in plugin header |
| `--description` | - | Description in plugin header |

**Examples:**
```bash
# Basic plugin
esp create "MyMod.esp"

# Light plugin with metadata
esp create "MyMod.esp" --light --author "YourName" --description "My awesome mod"

# Specify output directory
esp create "MyMod.esp" --output "./dist"
```

---

### info

Get information about an existing plugin.

```bash
esp info <plugin>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |

**Output includes:**
- Filename and path
- File size
- Light/Master flags
- Author (if set)
- Master file dependencies
- Record counts by type

**Example:**
```bash
esp info "MyMod.esp"
```

---

### add-quest

Add a quest record to a plugin.

```bash
esp add-quest <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the quest |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--start-enabled` | false | Quest starts when game loads |
| `--run-once` | false | Quest runs only once |
| `--priority` | 50 | Quest priority (0-255) |

**Examples:**
```bash
# Basic quest
esp add-quest "MyMod.esp" "MyMod_MainQuest" --name "The Main Quest"

# Start-enabled quest (requires SEQ file)
esp add-quest "MyMod.esp" "MyMod_InitQuest" --name "Init Quest" --start-enabled --run-once
```

---

### add-spell

Add a spell record to a plugin.

```bash
esp add-spell <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the spell |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--type` | `spell` | Type: `spell`, `power`, `lesser-power`, `ability` |
| `--cost` | 0 | Base magicka cost |
| `--effect` | - | Effect preset (see below) |
| `--magnitude` | 25 | Effect magnitude |
| `--duration` | 0 | Effect duration in seconds (0 = instant) |

**Effect Presets:**
| Preset | Description |
|--------|-------------|
| `damage-health` | Deal damage to target's health |
| `restore-health` | Restore target's health |
| `damage-magicka` | Drain target's magicka |
| `restore-magicka` | Restore target's magicka |
| `damage-stamina` | Drain target's stamina |
| `restore-stamina` | Restore target's stamina |
| `fortify-health` | Temporarily increase max health |
| `fortify-magicka` | Temporarily increase max magicka |
| `fortify-stamina` | Temporarily increase max stamina |
| `fortify-armor` | Increase damage resistance |
| `fortify-attack` | Increase attack damage |

**Examples:**
```bash
# Damage spell - deals 50 fire damage
esp add-spell "MyMod.esp" "MyMod_Fireball" --name "Fireball" --effect damage-health --magnitude 50 --cost 45

# Healing spell - restores 30 health
esp add-spell "MyMod.esp" "MyMod_Heal" --name "Minor Heal" --effect restore-health --magnitude 30 --cost 25

# Buff spell - +50 health for 60 seconds
esp add-spell "MyMod.esp" "MyMod_Fortify" --name "Fortify Health" --effect fortify-health --magnitude 50 --duration 60 --cost 80

# Power (daily ability) - restore all magicka
esp add-spell "MyMod.esp" "MyMod_RacialPower" --name "Ancient Power" --type power --effect restore-magicka --magnitude 500

# Ability (passive) - constant 25% armor bonus
esp add-spell "MyMod.esp" "MyMod_Passive" --name "Tough Skin" --type ability --effect fortify-armor --magnitude 25
```

**Note:** Without `--effect`, the spell will exist but do nothing. Always specify an effect.

---

### add-global

Add a global variable to a plugin.

```bash
esp add-global <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the global |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--type` | `float` | Type: `short`, `long`, `float` |
| `--value` | 0 | Initial value |

**Examples:**
```bash
# Float global (for multipliers, percentages)
esp add-global "MyMod.esp" "MyMod_DamageMultiplier" --type float --value 1.5

# Integer global (for counts, flags)
esp add-global "MyMod.esp" "MyMod_Enabled" --type long --value 1

# Short global (for small values)
esp add-global "MyMod.esp" "MyMod_Counter" --type short --value 0
```

---

### add-weapon

Add a weapon record to a plugin.

```bash
esp add-weapon <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the weapon |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--type` | `sword` | Type (see below) |
| `--damage` | 10 | Base damage |
| `--value` | 100 | Gold value |
| `--weight` | 5 | Weight |
| `--model` | - | Model path or preset |

**Weapon Types:**
- `sword` - One-handed sword
- `greatsword` - Two-handed sword
- `dagger` - Dagger
- `waraxe` - One-handed axe
- `battleaxe` - Two-handed axe
- `mace` - One-handed mace
- `warhammer` - Two-handed hammer
- `bow` - Bow
- `crossbow` - Crossbow
- `staff` - Staff

**Model Presets:**
- `iron-sword` - Iron sword model
- `steel-sword` - Steel sword model
- `iron-dagger` - Iron dagger model
- `hunting-bow` - Hunting bow model

**Important:** Without `--model`, the weapon will be invisible in-game.

**Examples:**
```bash
# Sword with vanilla iron sword model
esp add-weapon "MyMod.esp" "MyMod_Sword" --name "Blade of Testing" --type sword --damage 25 --model iron-sword

# Custom model path
esp add-weapon "MyMod.esp" "MyMod_DaedricBlade" --name "Daedric Blade" --type sword --damage 50 --model "Weapons\Daedric\DaedricSword.nif"

# Bow
esp add-weapon "MyMod.esp" "MyMod_Bow" --name "Hunter's Bow" --type bow --damage 15 --model hunting-bow
```

---

### add-armor

Add an armor record to a plugin.

```bash
esp add-armor <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the armor |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--type` | `light` | Type: `light`, `heavy`, `clothing` |
| `--slot` | `body` | Slot: `head`, `body`, `hands`, `feet`, `shield` |
| `--rating` | 10 | Armor rating |
| `--value` | 100 | Gold value |
| `--model` | - | Model path or preset |

**Model Presets:**
- `iron-cuirass` - Iron cuirass model
- `iron-helmet` - Iron helmet model
- `iron-gauntlets` - Iron gauntlets model
- `iron-boots` - Iron boots model
- `iron-shield` - Iron shield model

**Important:** Without `--model`, the armor will be invisible in-game.

**Examples:**
```bash
# Heavy armor cuirass
esp add-armor "MyMod.esp" "MyMod_Cuirass" --name "Steel Plate" --type heavy --slot body --rating 40 --model iron-cuirass

# Light armor helmet
esp add-armor "MyMod.esp" "MyMod_Hood" --name "Thief's Hood" --type light --slot head --rating 10 --model iron-helmet
```

---

### add-npc

Add an NPC record to a plugin.

```bash
esp add-npc <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the NPC |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--level` | 1 | NPC level |
| `--female` | false | NPC is female |
| `--essential` | false | NPC cannot be killed |
| `--unique` | false | NPC is unique |

**Note:** This creates an NPC record structure. NPCs need race/face data to be visible in-game. Best used for modifying existing NPCs via scripts.

**Examples:**
```bash
# Basic NPC
esp add-npc "MyMod.esp" "MyMod_Guard" --name "Test Guard" --level 25

# Essential unique NPC
esp add-npc "MyMod.esp" "MyMod_Merchant" --name "Bob the Merchant" --level 10 --essential --unique
```

---

### add-book

Add a book record to a plugin.

```bash
esp add-book <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the book |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--text` | - | Book content |
| `--value` | 10 | Gold value |
| `--weight` | 1 | Weight |

**Examples:**
```bash
# Lore book
esp add-book "MyMod.esp" "MyMod_Lore" --name "The History of Testing" --text "Long ago, in a land far away..." --value 50

# Journal
esp add-book "MyMod.esp" "MyMod_Journal" --name "Adventurer's Journal" --text "Day 1: I set out from Whiterun today..." --value 5 --weight 0.5
```

---

### add-leveled-item

Add a leveled item list to a plugin for random loot distribution.

```bash
esp add-leveled-item <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the leveled item |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--chance-none` | 0 | Percentage chance (0-100) the list returns nothing |
| `--add-entry` | - | Add entry in format: `item,level,count` (can use multiple times) |
| `--preset` | - | Apply preset configuration (see below) |
| `--calculate-each` | false | Roll separately for each item count |
| `--use-all` | false | Give all items in list |
| `--dry-run` | false | Preview changes without saving |

**Presets:**
| Preset | Description |
|--------|-------------|
| `low-treasure` | 25% chance none, good for starter areas |
| `medium-treasure` | 15% chance none, standard dungeon loot |
| `high-treasure` | 5% chance none, boss chest rewards |
| `guaranteed-loot` | 0% chance none, always gives all items |

**Examples:**
```bash
# Using preset for treasure chest
esp add-leveled-item "MyMod.esp" "MyMod_TreasureChest" --preset low-treasure

# Custom list with entries
esp add-leveled-item "MyMod.esp" "MyMod_BossLoot" --chance-none 5 --add-entry "GoldBase,1,100" --add-entry "LockPick,5,3"

# Guaranteed multi-item drop
esp add-leveled-item "MyMod.esp" "MyMod_QuestReward" --chance-none 0 --use-all --add-entry "WeaponSword,10,1"
```

---

### add-form-list

Add a form list to a plugin for script property collections.

```bash
esp add-form-list <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the form list |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--add-form` | - | Add form by EditorID or FormKey (can use multiple times) |
| `--dry-run` | false | Preview changes without saving |

**Examples:**
```bash
# Add vanilla forms by FormKey
esp add-form-list "MyMod.esp" "MyMod_MetalKeywords" --add-form "Skyrim.esm:0x000896" --add-form "Skyrim.esm:0x000897"

# Add mod-specific forms by EditorID
esp add-form-list "MyMod.esp" "MyMod_CustomWeapons" --add-form "MyMod_Sword01" --add-form "MyMod_Sword02"

# Use in script: FormList Property MyList Auto
# Set property: --property MyList --value "MyMod.esp|0x800"
```

---

### add-encounter-zone

Add an encounter zone to control level scaling in locations.

```bash
esp add-encounter-zone <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the encounter zone |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--min-level` | 1 | Minimum enemy level |
| `--max-level` | 0 | Maximum enemy level (0 = unlimited scaling) |
| `--never-resets` | false | Enemies stay defeated permanently |
| `--disable-combat-boundary` | false | NPCs can pursue player anywhere |
| `--preset` | - | Apply preset configuration (see below) |
| `--dry-run` | false | Preview changes without saving |

**Presets:**
| Preset | Description |
|--------|-------------|
| `low-level` | Min 1, Max 10 - starter areas, tutorial content |
| `mid-level` | Min 10, Max 30 - standard dungeons, mid-game |
| `high-level` | Min 30, Max 50 - end-game content, boss fights |
| `scaling` | Min 1, Max unlimited - quest content that works at any level |

**Examples:**
```bash
# Starter dungeon
esp add-encounter-zone "MyMod.esp" "MyMod_StarterCave" --preset low-level

# End-game raid
esp add-encounter-zone "MyMod.esp" "MyMod_BossLair" --min-level 30 --max-level 50 --never-resets

# Fully scaling quest zone
esp add-encounter-zone "MyMod.esp" "MyMod_QuestZone" --preset scaling
```

---

### add-location

Add a location record for named areas, quest markers, and fast travel points.

```bash
esp add-location <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the location |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name shown to player |
| `--parent-location` | - | Parent location FormKey or EditorID |
| `--add-keyword` | - | Add location keyword (can use multiple times) |
| `--preset` | - | Apply preset with keyword (see below) |
| `--dry-run` | false | Preview changes without saving |

**Presets:**
| Preset | Keyword | Use Case |
|--------|---------|----------|
| `inn` | LocTypeInn | Taverns, inns, drinking establishments |
| `city` | LocTypeCity | Major walled settlements, capitals |
| `dungeon` | LocTypeDungeon | Caves, ruins, underground areas |
| `dwelling` | LocTypeDwelling | Player homes, NPC houses |

**Examples:**
```bash
# Inn location
esp add-location "MyMod.esp" "MyMod_RustyTankard" --name "The Rusty Tankard" --preset inn

# Player home with parent
esp add-location "MyMod.esp" "MyMod_PlayerHome" --name "Cozy Cottage" --preset dwelling --parent-location "WhiterunHoldLocation"

# Custom dungeon
esp add-location "MyMod.esp" "MyMod_AncientRuins" --name "Forgotten Crypt" --preset dungeon
```

---

### add-outfit

Add an outfit record to define NPC equipment sets.

```bash
esp add-outfit <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the outfit |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--add-item` | - | Add armor or weapon by EditorID (can use multiple times) |
| `--preset` | - | Apply preset outfit (see below) |
| `--dry-run` | false | Preview changes without saving |

**Presets:**
| Preset | Contents |
|--------|----------|
| `guard` | Iron armor + sword + shield (basic guard outfit) |
| `farmer` | Farmer clothes + roughspun tunic (civilian clothing) |
| `mage` | Mage robes + hood (spellcaster outfit) |
| `thief` | Leather armor set (rogue/thief outfit) |

**Examples:**
```bash
# Guard outfit with preset
esp add-outfit "MyMod.esp" "MyMod_TownGuard" --preset guard

# Custom outfit
esp add-outfit "MyMod.esp" "MyMod_CustomOutfit" --add-item "ArmorIronCuirass" --add-item "WeaponIronSword" --add-item "ArmorIronShield"

# Farmer outfit
esp add-outfit "MyMod.esp" "MyMod_Villager" --preset farmer
```

---

### add-perk

Add a perk record to a plugin.

```bash
esp add-perk <plugin> <editorId> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |
| `editorId` | Unique Editor ID for the perk |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--name` | - | Display name |
| `--description` | - | Perk description |
| `--playable` | false | Perk can be selected by player |
| `--hidden` | false | Perk is hidden |
| `--effect` | - | Effect preset (see below) |
| `--bonus` | 25 | Bonus percentage |

**Effect Presets:**
| Preset | Description |
|--------|-------------|
| `weapon-damage` | Increase weapon damage by X% |
| `damage-reduction` | Reduce incoming damage by X% |
| `armor` | Increase armor rating by X% |
| `spell-cost` | Reduce spell cost by X% |
| `spell-power` | Increase spell magnitude by X% |
| `spell-duration` | Increase spell duration by X% |
| `sneak-attack` | Increase sneak attack multiplier |
| `pickpocket` | Increase pickpocket chance by X% |
| `prices` | Improve buying/selling prices by X% |

**Examples:**
```bash
# Combat perk - +25% weapon damage
esp add-perk "MyMod.esp" "MyMod_Damage" --name "Heavy Hitter" --description "Deal 25% more damage" --effect weapon-damage --bonus 25 --playable

# Defensive perk - 15% damage reduction
esp add-perk "MyMod.esp" "MyMod_Tank" --name "Thick Skin" --description "Take 15% less damage" --effect damage-reduction --bonus 15 --playable

# Magic perk - 20% cheaper spells
esp add-perk "MyMod.esp" "MyMod_Efficient" --name "Efficient Casting" --description "Spells cost 20% less" --effect spell-cost --bonus 20 --playable

# Stealth perk - 50% higher sneak attack damage
esp add-perk "MyMod.esp" "MyMod_Assassin" --name "Assassin's Strike" --description "Sneak attacks deal 50% more damage" --effect sneak-attack --bonus 50 --playable

# Hidden perk (for internal mechanics)
esp add-perk "MyMod.esp" "MyMod_InternalPerk" --name "Internal Effect" --effect damage-reduction --bonus 10 --hidden
```

**Note:** Without `--effect`, the perk will exist but do nothing. Always specify an effect.

---

### attach-script

Attach a Papyrus script to a quest.

```bash
esp attach-script <plugin> --quest <questId> --script <scriptName>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--quest` | Editor ID of the quest |
| `--script` | Name of the script (without .pex) |

**Example:**
```bash
esp attach-script "MyMod.esp" --quest "MyMod_MainQuest" --script "MyMod_MainQuestScript"
```

---

### generate-seq

Generate a SEQ file for start-enabled quests.

```bash
esp generate-seq <plugin> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--output`, `-o` | `.` | Output directory |

**Note:** SEQ files are required for quests with `--start-enabled`. Place the SEQ file in `Data/SEQ/`.

**Example:**
```bash
esp generate-seq "MyMod.esp" --output "./SEQ"
```

---

### list-masters

List master file dependencies.

```bash
esp list-masters <plugin>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `plugin` | Path to the plugin file |

**Example:**
```bash
esp list-masters "MyMod.esp"
```

---

### merge

Merge records from one plugin into another.

```bash
esp merge <source> <target> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `source` | Source plugin to copy from |
| `target` | Target plugin to copy into |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--output` | target | Output path (defaults to overwriting target) |

**Example:**
```bash
# Merge Source.esp into Target.esp, output to Merged.esp
esp merge "Source.esp" "Target.esp" --output "Merged.esp"
```

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
esp info "MyMod.esp" --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "fileName": "MyMod.esp",
    "isLight": true,
    "totalRecords": 5
  }
}
```

**Error response:**
```json
{
  "success": false,
  "error": "File not found",
  "suggestions": ["Check the file path"]
}
```
