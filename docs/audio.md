# Audio Module Reference

The Audio module handles Skyrim audio file operations including FUZ, XWM, and WAV formats.

## Overview

Skyrim uses several audio formats:

| Format | Extension | Purpose |
|--------|-----------|---------|
| FUZ | .fuz | Combined voice audio + lip sync |
| XWM | .xwm | Compressed audio (xWMA codec) |
| WAV | .wav | Uncompressed audio |
| LIP | .lip | Lip sync data |

Voice files are typically stored as FUZ (XWM audio + LIP lip sync combined).

## Commands

### info

Get information about an audio file.

```bash
audio info <audio>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `audio` | Path to audio file (.fuz, .xwm, or .wav) |

**Output includes:**
- File type
- File size
- Audio data size
- Lip sync presence/size (for FUZ)
- Sample rate, channels, bits per sample (when available)

**Examples:**
```bash
audio info "./Sound/Voice/MyMod/NPC_Line.fuz"
audio info "./Sound/MySound.xwm"
audio info "./Source/voice.wav"
```

---

### extract-fuz

Extract a FUZ file into its XWM and LIP components.

```bash
audio extract-fuz <fuz> --output <dir>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `fuz` | Path to FUZ file |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output directory |

**Output files:**
- `filename.xwm` - Audio data
- `filename.lip` - Lip sync data (if present)

**Example:**
```bash
audio extract-fuz "./Sound/Voice/MyMod/Line01.fuz" --output "./Extracted"
# Creates: ./Extracted/Line01.xwm and ./Extracted/Line01.lip
```

---

### create-fuz

Create a FUZ file from XWM audio and optional LIP data.

```bash
audio create-fuz <xwm> --output <file> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `xwm` | Path to XWM audio file |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output FUZ file path |

**Optional:**
| Option | Description |
|--------|-------------|
| `--lip` | Path to LIP file |

**Examples:**
```bash
# With lip sync
audio create-fuz "./Audio/Line01.xwm" --lip "./Audio/Line01.lip" --output "./Sound/Voice/MyMod/Line01.fuz"

# Without lip sync
audio create-fuz "./Audio/Sound.xwm" --output "./Sound/FX/Sound.fuz"
```

---

### wav-to-xwm

Convert WAV audio to XWM format.

```bash
audio wav-to-xwm <wav> --output <file>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `wav` | Path to WAV file |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--output`, `-o` | Output XWM file path |

**WAV Requirements:**
- PCM format
- 16-bit
- Mono or stereo
- 44100 Hz recommended for voice

**Example:**
```bash
audio wav-to-xwm "./Source/voice_line.wav" --output "./Audio/voice_line.xwm"
```

---

## Audio Workflow

### Voice Line Workflow

Complete workflow for adding voice lines:

```bash
# 1. Record/obtain WAV file (44100 Hz, 16-bit, mono recommended for voice)

# 2. Convert to XWM
audio wav-to-xwm "./Source/Line01.wav" --output "./Audio/Line01.xwm"

# 3. Generate LIP file (requires FaceFX or external tool - not included)
#    Or use existing LIP from similar dialogue

# 4. Create FUZ
audio create-fuz "./Audio/Line01.xwm" --lip "./Audio/Line01.lip" --output "./Sound/Voice/MyMod/NPC/Line01.fuz"
```

### Sound Effect Workflow

For non-voice sounds (no lip sync needed):

```bash
# 1. Convert WAV to XWM
audio wav-to-xwm "./Source/effect.wav" --output "./Sound/FX/MyMod/effect.xwm"

# Or package as FUZ without LIP
audio create-fuz "./Audio/effect.xwm" --output "./Sound/FX/MyMod/effect.fuz"
```

---

## FUZ File Format

FUZ files have the following structure:

```
Header:
  Magic: "FUZE" (4 bytes)
  Version: uint32
  LIP size: uint32

Data:
  LIP data: [LIP size bytes]
  XWM data: [remaining bytes]
```

---

## Voice File Organization

Skyrim expects voice files in specific locations:

```
Data/
  Sound/
    Voice/
      MyMod.esp/
        NPC_EditorID/
          DialogueTopic_ResponseNumber.fuz
```

Example:
```
Data/Sound/Voice/MyMod.esp/MerchantBob/DialogueMerchantGreeting_00001234_1.fuz
```

---

## Limitations

This module can:
- Read FUZ/XWM/WAV file information
- Extract FUZ to components
- Create FUZ from components
- Convert WAV to XWM

This module cannot:
- Generate LIP (lip sync) files
- Convert XWM back to WAV
- Edit audio content

For LIP generation, use:
- **FaceFX** (included with Creation Kit)
- **LipGen** tools

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
audio info "./Sound/Voice/MyMod/Line01.fuz" --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "fileName": "Line01.fuz",
    "type": "FUZ",
    "fileSize": 45678,
    "audioSize": 42000,
    "hasLipSync": true,
    "lipSyncSize": 3678,
    "sampleRate": 44100,
    "channels": 1,
    "bitsPerSample": 16
  }
}
```
