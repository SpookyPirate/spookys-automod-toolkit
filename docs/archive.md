# Archive Module Reference

The Archive module handles BSA/BA2 archive operations including reading, extracting, and creating archives.

## External Tools

| Tool | Purpose | Auto-Download |
|------|---------|---------------|
| BSArch | Create/extract archives | No - manual install required |

### Installing BSArch

1. Download xEdit from [GitHub releases](https://github.com/TES5Edit/TES5Edit/releases)
2. Extract the archive
3. Copy `BSArch.exe` to `tools/bsarch/` in the toolkit directory

## Commands

### info

Get information about an archive by reading its header.

```bash
archive info <archive>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `archive` | Path to BSA/BA2 file |

**Output includes:**
- Archive type (BSA or BA2)
- Version
- File count
- Total size

**Example:**
```bash
archive info "MyMod.bsa"
```

---

### list

List files in an archive.

```bash
archive list <archive> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `archive` | Path to BSA/BA2 file |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--filter`, `-f` | - | Filter pattern (e.g., `*.nif`, `textures/*`) |
| `--limit` | 100 | Max files to list (0 = all) |

**Examples:**
```bash
# List first 100 files
archive list "MyMod.bsa"

# List all NIF files
archive list "MyMod.bsa" --filter "*.nif"

# List all files
archive list "MyMod.bsa" --limit 0
```

---

### extract

Extract files from an archive.

```bash
archive extract <archive> --output <dir> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `archive` | Path to BSA/BA2 file |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output directory |

**Optional:**
| Option | Description |
|--------|-------------|
| `--filter`, `-f` | Filter pattern for files to extract |

**Requires:** BSArch tool installed

**Examples:**
```bash
# Extract entire archive
archive extract "MyMod.bsa" --output "./Extracted"

# Extract only textures
archive extract "MyMod.bsa" --output "./Extracted" --filter "textures/*"

# Extract only meshes
archive extract "MyMod.bsa" --output "./Extracted" --filter "*.nif"
```

---

### create

Create a new BSA archive from a directory.

```bash
archive create <directory> --output <file> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `directory` | Source directory to archive |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output archive path |

**Optional:**
| Option | Default | Description |
|--------|---------|-------------|
| `--compress` | true | Compress archive contents |
| `--game` | `sse` | Game type (see below) |

**Game Types:**
| Value | Game |
|-------|------|
| `sse` | Skyrim Special Edition |
| `le` | Skyrim Legendary Edition |
| `fo4` | Fallout 4 |
| `fo76` | Fallout 76 |

**Requires:** BSArch tool installed

**Directory Structure:**
The source directory should mirror the Skyrim Data folder structure:
```
MyModData/
  meshes/
    mymod/
      weapon.nif
  textures/
    mymod/
      weapon.dds
  scripts/
    MyMod_Script.pex
```

**Examples:**
```bash
# Create compressed SSE archive
archive create "./MyModData" --output "MyMod.bsa"

# Create uncompressed archive
archive create "./MyModData" --output "MyMod.bsa" --compress false

# Create LE archive
archive create "./MyModData" --output "MyMod.bsa" --game le
```

---

### status

Check if BSArch tool is available.

```bash
archive status
```

**Output:**
- BSArch availability
- Path (if available)
- Installation instructions (if not available)

**Example:**
```bash
archive status
```

---

## Archive Types

### BSA (Bethesda Softworks Archive)
- Used by: Skyrim LE, Skyrim SE
- Magic: `BSA\0`
- Supports compression

### BA2 (Bethesda Archive 2)
- Used by: Fallout 4, Fallout 76
- Magic: `BTDX`
- Supports different storage types (General, Textures)

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
archive info "MyMod.bsa" --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "fileName": "MyMod.bsa",
    "type": "BSA",
    "version": "105",
    "fileCount": 150,
    "fileSize": 52428800
  }
}
```

**Error response (BSArch not found):**
```json
{
  "success": false,
  "error": "BSArch not found",
  "suggestions": [
    "Download xEdit from: https://github.com/TES5Edit/TES5Edit/releases",
    "Extract BSArch.exe from the archive",
    "Place bsarch.exe in: tools/bsarch"
  ]
}
```
