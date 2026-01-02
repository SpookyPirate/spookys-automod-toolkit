# Papyrus Module Reference

The Papyrus module handles script compilation, decompilation, validation, and template generation.

## External Tools

This module uses external tools that are auto-downloaded on first use:

| Tool | Purpose |
|------|---------|
| papyrus-compiler | Compiles PSC to PEX |
| Champollion | Decompiles PEX to PSC |

## Commands

### status

Check the status of Papyrus tools.

```bash
papyrus status
```

**Output includes:**
- Compiler availability and path
- Decompiler availability and path

**Example:**
```bash
papyrus status
```

---

### download

Download Papyrus compiler and decompiler tools.

```bash
papyrus download
```

Downloads tools to the `tools/` directory automatically.

---

### compile

Compile Papyrus source files to PEX.

```bash
papyrus compile <source> --output <dir> --headers <dir> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `source` | Path to PSC file or directory |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output directory for PEX files |
| `--headers`, `-i` | Directory containing script headers |

**Optional:**
| Option | Default | Description |
|--------|---------|-------------|
| `--optimize`, `-O` | true | Enable optimization |

**Headers Path:**
The headers directory should contain vanilla Skyrim script sources (.psc files).

**Recommended:** Use the toolkit's dedicated headers directory:
```bash
--headers "./skyrim-script-headers"
```

See the main README "Papyrus Script Headers" section for installation instructions.

**Alternative:** Reference Creation Kit directly:
- Steam: `Steam/steamapps/common/Skyrim Special Edition/Data/Scripts/Source`
- GOG: `GOG Galaxy/Games/Skyrim Special Edition/Data/Scripts/Source`

**Examples:**
```bash
# Compile single file (using toolkit headers)
papyrus compile "./Scripts/Source/MyScript.psc" --output "./Scripts" --headers "./skyrim-script-headers"

# Compile directory (using toolkit headers)
papyrus compile "./Scripts/Source" --output "./Scripts" --headers "./skyrim-script-headers"

# Without optimization
papyrus compile "./Scripts/Source" --output "./Scripts" --headers "./skyrim-script-headers" --optimize false

# Using Creation Kit headers directly
papyrus compile "./Scripts/Source" --output "./Scripts" --headers "C:/Program Files (x86)/Steam/steamapps/common/Skyrim Special Edition/Data/Scripts/Source"
```

---

### decompile

Decompile PEX files to Papyrus source.

```bash
papyrus decompile <pex> --output <dir>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `pex` | Path to PEX file or directory |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output directory for PSC files |

**Examples:**
```bash
# Decompile single file
papyrus decompile "./Scripts/MyScript.pex" --output "./Decompiled"

# Decompile directory
papyrus decompile "./Scripts" --output "./Decompiled"
```

---

### validate

Validate Papyrus source file syntax.

```bash
papyrus validate <psc>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `psc` | Path to PSC file |

**Output includes:**
- Validation status (pass/fail)
- Errors (if any)
- Warnings (if any)

**Example:**
```bash
papyrus validate "./Scripts/Source/MyScript.psc"
```

---

### generate

Generate a Papyrus script template.

```bash
papyrus generate --name <name> [options]
```

**Required Options:**
| Option | Description |
|--------|-------------|
| `--name` | Script name (without extension) |

**Optional:**
| Option | Default | Description |
|--------|---------|-------------|
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

**Examples:**
```bash
# Quest script
papyrus generate --name "MyMod_MainQuestScript" --extends Quest --output "./Scripts/Source"

# Actor script
papyrus generate --name "MyMod_ActorScript" --extends Actor --output "./Scripts/Source"

# With description
papyrus generate --name "MyMod_Script" --extends Quest --description "Main quest controller" --output "./Scripts/Source"
```

**Generated Template Example:**
```papyrus
ScriptName MyMod_MainQuestScript extends Quest
{Main quest controller}

;-- Properties --

;-- Events --
Event OnInit()
    ; Initialization code here
EndEvent

;-- Functions --
```

---

## Workflow Example

Complete workflow for creating and compiling a quest script:

```bash
# 1. Generate script template
papyrus generate --name "MyMod_QuestScript" --extends Quest --output "./Scripts/Source"

# 2. Edit the script (manually or via AI)

# 3. Validate syntax
papyrus validate "./Scripts/Source/MyMod_QuestScript.psc"

# 4. Compile
papyrus compile "./Scripts/Source" --output "./Scripts" --headers "./skyrim-script-headers"

# 5. Attach to quest
esp attach-script "MyMod.esp" --quest "MyMod_MainQuest" --script "MyMod_QuestScript"
```

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
papyrus status --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "compilerAvailable": true,
    "compilerPath": "tools/papyrus-compiler/papyrus-compiler.exe",
    "decompilerAvailable": true,
    "decompilerPath": "tools/champollion/Champollion.exe"
  }
}
```

**Compilation error response:**
```json
{
  "success": false,
  "error": "Compilation failed",
  "errorContext": "Line 15: Missing EndFunction",
  "suggestions": ["Add 'EndFunction' after line 15"]
}
```
