# MCM Module Reference

The MCM module handles creation and editing of MCM Helper configuration files for mod settings menus.

## Overview

MCM (Mod Configuration Menu) allows mods to have in-game settings menus. This module generates JSON configuration files compatible with [MCM Helper](https://www.nexusmods.com/skyrimspecialedition/mods/53000).

## Commands

### create

Create a new MCM configuration file.

```bash
mcm create <modName> <displayName> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `modName` | Internal mod name (no spaces, used for file paths) |
| `displayName` | Display name shown in MCM menu |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--output`, `-o` | `./config.json` | Output file path |

**Examples:**
```bash
# Basic config
mcm create "MyMod" "My Awesome Mod" --output "./MCM/config.json"

# Custom path
mcm create "MyMod" "My Awesome Mod" --output "./Data/MCM/config/MyMod/config.json"
```

---

### info

Get information about an MCM config file.

```bash
mcm info <config>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `config` | Path to config.json file |

**Output includes:**
- Mod name and display name
- Minimum MCM version
- Page count
- Control count
- Page details (with `--verbose`)

**Example:**
```bash
mcm info "./MCM/config.json"
```

---

### validate

Validate an MCM config file.

```bash
mcm validate <config>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `config` | Path to config.json file |

**Output includes:**
- Validation status (pass/fail)
- Errors (if any)
- Warnings (if any)

**Example:**
```bash
mcm validate "./MCM/config.json"
```

---

### add-toggle

Add a toggle (checkbox) control to MCM config.

```bash
mcm add-toggle <config> <id> <text> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `config` | Path to config.json file |
| `id` | Control identifier (e.g., `bEnabled`) |
| `text` | Display text |

**Options:**
| Option | Description |
|--------|-------------|
| `--help-text` | Help text shown on hover |
| `--page` | Target page name |

**Examples:**
```bash
# Basic toggle
mcm add-toggle "./config.json" "bEnabled" "Enable Mod"

# With help text
mcm add-toggle "./config.json" "bDebugMode" "Debug Mode" --help-text "Show debug messages in console"

# On specific page
mcm add-toggle "./config.json" "bFeature" "Enable Feature" --page "Features"
```

---

### add-slider

Add a slider control to MCM config.

```bash
mcm add-slider <config> <id> <text> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `config` | Path to config.json file |
| `id` | Control identifier (e.g., `fDamageMultiplier`) |
| `text` | Display text |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--min` | 0 | Minimum value |
| `--max` | 100 | Maximum value |
| `--step` | 1 | Step increment |

**Examples:**
```bash
# Basic slider
mcm add-slider "./config.json" "fDamage" "Damage Multiplier" --min 0.5 --max 2.0 --step 0.1

# Percentage slider
mcm add-slider "./config.json" "fChance" "Chance %" --min 0 --max 100 --step 5

# Integer slider
mcm add-slider "./config.json" "iCount" "Item Count" --min 1 --max 10 --step 1
```

---

## MCM Helper Integration

### File Structure

MCM Helper expects files in specific locations:

```
Data/
  MCM/
    config/
      MyMod/
        config.json     # Main config
        settings.ini    # Default values (optional)
    translations/
      MyMod_english.txt # Translations (optional)
```

### Config JSON Format

Generated config.json structure:

```json
{
  "modName": "MyMod",
  "displayName": "My Awesome Mod",
  "minMcmVersion": 7,
  "pages": [
    {
      "pageDisplayName": "Main",
      "content": [
        {
          "type": "toggle",
          "id": "bEnabled",
          "text": "Enable Mod"
        },
        {
          "type": "slider",
          "id": "fDamage",
          "text": "Damage Multiplier",
          "min": 0.5,
          "max": 2.0,
          "step": 0.1
        }
      ]
    }
  ]
}
```

### Control Types

| Type | Description | Properties |
|------|-------------|------------|
| toggle | Checkbox | id, text, help |
| slider | Value slider | id, text, min, max, step |
| dropdown | Dropdown list | id, text, options |
| keymap | Key binding | id, text |
| text | Text input | id, text |

---

## Workflow Example

Complete workflow for creating an MCM menu:

```bash
# 1. Create config
mcm create "MyMod" "My Awesome Mod" --output "./MCM/config/MyMod/config.json"

# 2. Add controls
mcm add-toggle "./MCM/config/MyMod/config.json" "bEnabled" "Enable Mod" --help-text "Toggle the mod on/off"
mcm add-slider "./MCM/config/MyMod/config.json" "fDamage" "Damage Multiplier" --min 0.5 --max 2.0 --step 0.1
mcm add-toggle "./MCM/config/MyMod/config.json" "bDebug" "Debug Mode"

# 3. Validate
mcm validate "./MCM/config/MyMod/config.json"

# 4. Copy to Skyrim Data folder
```

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
mcm info "./config.json" --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "modName": "MyMod",
    "displayName": "My Awesome Mod",
    "minMcmVersion": 7,
    "pageCount": 1,
    "controlCount": 3,
    "pages": [
      {
        "name": "Main",
        "controlCount": 3
      }
    ]
  }
}
```
