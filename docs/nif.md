# NIF Module Reference

The NIF module handles reading and basic manipulation of NIF (NetImmerse Format) 3D mesh files.

## Overview

NIF files are the 3D model format used by Skyrim for meshes (weapons, armor, architecture, etc.). This module provides read capabilities and basic transformations.

**Note:** This module cannot create new meshes from scratch. For that, use Blender with the NifTools addon.

## Commands

### info

Get information about a NIF file.

```bash
nif info <nif>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `nif` | Path to the NIF file |

**Output includes:**
- Filename
- File size
- Header string (NIF version info)
- Version number

**Example:**
```bash
nif info "./Meshes/Weapons/Iron/IronSword.nif"
```

---

### textures

List textures referenced in a NIF file.

```bash
nif textures <nif>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `nif` | Path to the NIF file |

**Output:**
- List of texture paths referenced in the mesh

**Example:**
```bash
nif textures "./Meshes/Weapons/Iron/IronSword.nif"
```

**Example output:**
```
Textures (3):
  textures\weapons\iron\ironsword.dds
  textures\weapons\iron\ironsword_n.dds
  textures\weapons\iron\ironsword_s.dds
```

---

### scale

Scale a NIF mesh uniformly.

```bash
nif scale <nif> <factor> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `nif` | Path to the NIF file |
| `factor` | Scale factor (e.g., 1.5 for 150%, 0.5 for 50%) |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--output`, `-o` | input file | Output file path |

**Examples:**
```bash
# Scale to 150%, overwrite original
nif scale "./Meshes/weapon.nif" 1.5

# Scale to 50%, save to new file
nif scale "./Meshes/weapon.nif" 0.5 --output "./Meshes/weapon_small.nif"

# Double size
nif scale "./Meshes/weapon.nif" 2.0 --output "./Meshes/weapon_large.nif"
```

---

### copy

Copy a NIF file (validates format during copy).

```bash
nif copy <nif> --output <file>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `nif` | Path to source NIF file |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output file path |

**Example:**
```bash
nif copy "./Meshes/weapon.nif" --output "./Meshes/weapon_copy.nif"
```

---

## NIF Format Information

### Skyrim NIF Versions

| Game | NIF Version | Header String |
|------|-------------|---------------|
| Skyrim LE | 20.2.0.7 | NIF... |
| Skyrim SE | 20.2.0.7 | BSFadeNode |
| Fallout 4 | 20.2.0.7 | BSFadeNode |

### Common Node Types

| Node | Purpose |
|------|---------|
| BSFadeNode | Root node for meshes |
| NiTriShape | Triangle geometry |
| BSTriShape | Optimized triangle geometry (SSE) |
| BSLightingShaderProperty | Material/shader info |
| NiSkinInstance | Skinning for animated meshes |

### Texture Slots

| Slot | Suffix | Purpose |
|------|--------|---------|
| Diffuse | none / _d | Base color |
| Normal | _n | Normal map |
| Specular | _s | Specular/gloss |
| Glow | _g | Emissive/glow |
| Cube Map | _e | Environment map |

---

## Limitations

This module can:
- Read NIF file information
- List referenced textures
- Scale meshes uniformly
- Copy/validate NIF files

This module cannot:
- Create new meshes from scratch
- Edit mesh geometry (vertices, faces)
- Retexture meshes
- Create or edit rigging/skinning
- Convert between NIF versions

For advanced mesh editing, use:
- **Blender** + **NifTools** addon
- **NifSkope** for direct editing

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
nif info "./Meshes/weapon.nif" --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "fileName": "weapon.nif",
    "fileSize": 45678,
    "version": "20.2.0.7",
    "headerString": "Gamebryo File Format, Version 20.2.0.7"
  }
}
```

**Textures response:**
```json
{
  "success": true,
  "result": {
    "textures": [
      "textures\\weapons\\iron\\ironsword.dds",
      "textures\\weapons\\iron\\ironsword_n.dds"
    ]
  }
}
```
