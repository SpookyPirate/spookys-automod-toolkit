# Release Structure Guide

This document defines the proper structure for GitHub releases to ensure users can unzip and immediately use the toolkit.

## Purpose

Releases are **ready-to-use distributions** with pre-built binaries. Users unzip to their projects directory and can immediately run commands without building.

## Directory Structure

```
spookys-automod-toolkit-vX.Y.Z/
├── README.txt                          # Quick start guide
├── .claude/                            # Claude Code skills
│   └── skills/
│       ├── skyrim-archive/
│       ├── skyrim-audio/
│       ├── skyrim-esp/
│       ├── skyrim-mcm/
│       ├── skyrim-nif/
│       ├── skyrim-papyrus/
│       └── skyrim-skse/
└── spookys-automod-toolkit/
    ├── .gitignore
    ├── CHANGELOG.md
    ├── CLAUDE.md
    ├── CONTRIBUTING.md
    ├── README.md
    ├── SpookysAutomod.sln
    ├── docs/                           # User documentation
    ├── graphics/                       # Logos
    ├── src/                            # Source code WITH bin/Release/
    │   ├── SpookysAutomod.Cli/
    │   │   └── bin/Release/net8.0/     # ✓ Pre-built binaries
    │   ├── SpookysAutomod.Core/
    │   │   └── bin/Release/net8.0/     # ✓ Pre-built binaries
    │   └── ... (all modules with bin/Release/)
    └── templates/                      # SKSE templates
```

## What to INCLUDE

### Required Files
- README.txt (root level)
- .claude/skills/ (all skills)
- All source code (.cs, .csproj files)
- SpookysAutomod.sln
- CHANGELOG.md, CLAUDE.md, CONTRIBUTING.md, README.md
- docs/ (all documentation)
- graphics/ (logo)
- templates/ (SKSE templates)
- **bin/Release/** directories (pre-built binaries - CRITICAL)

### Pre-Built Binaries
- `src/*/bin/Release/net8.0/` - All Release build outputs
- Includes all DLLs, dependencies, and executables
- Users can run immediately without `dotnet build`

## What to EXCLUDE

### Development Artifacts
- `tests/` - Unit tests (not needed by end users)
- `bin/Debug/` - Debug builds
- `obj/` - All intermediate build files
- `.vs/`, `.idea/`, `.vscode/` - IDE caches
- `*.user` files

### Copyrighted Content
- `skyrim-script-headers/` - Bethesda copyright
- `scripts/` - Test scripts

### Git/Build Metadata
- `.git/` - Repository data
- `.github/` - Workflows
- `.papyrus/` - Compiler cache
- Root-level test files (*.csx, ConditionApiTest.*, temp/)

## README.txt Template

```txt
# Spooky's AutoMod Toolkit vX.Y.Z

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - The toolkit source code

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## Quick Start

See spookys-automod-toolkit/README.md for installation and usage instructions.
```

## Creating a Release

### Step 1: Build Release Binaries

```bash
cd path/to/repository
dotnet build SpookysAutomod.sln -c Release
```

### Step 2: Create Release Structure

```bash
# Clean previous build
rm -rf /c/tmp/release
mkdir -p /c/tmp/release/spookys-automod-toolkit-v1.X.X

# Copy .claude/
cp -r .claude /c/tmp/release/spookys-automod-toolkit-v1.X.X/

# Create README.txt (use template above)
cat > /c/tmp/release/spookys-automod-toolkit-v1.X.X/README.txt << 'EOF'
[README.txt content from template]
EOF

# Copy all files EXCEPT excluded directories
# Include bin/Release/, exclude bin/Debug/ and obj/
tar --exclude='.git' \
    --exclude='bin/Debug' \
    --exclude='obj' \
    --exclude='tests' \
    --exclude='.vs' \
    --exclude='.idea' \
    --exclude='.vscode' \
    --exclude='skyrim-script-headers' \
    --exclude='scripts' \
    --exclude='.papyrus' \
    --exclude='.claude' \
    --exclude='temp' \
    --exclude='*.csx' \
    --exclude='ConditionApiTest.*' \
    --exclude='RELEASE.md' \
    -cf - . | tar -xf - -C /c/tmp/release/spookys-automod-toolkit-v1.X.X/spookys-automod-toolkit
```

### Step 3: Clean Up Root Test Files

```bash
cd /c/tmp/release/spookys-automod-toolkit-v1.X.X/spookys-automod-toolkit
rm -f ConditionApiTest.cs ConditionApiTest.csproj test-*.csx RELEASE.md
rm -rf temp
```

### Step 4: Verify Structure

```bash
# Check bin/Release/ exists
ls src/SpookysAutomod.Cli/bin/Release/net8.0/*.dll

# Check excluded directories are gone
ls tests 2>/dev/null && echo "ERROR: tests/ still present"
ls bin/Debug 2>/dev/null && echo "ERROR: bin/Debug/ still present"
ls obj 2>/dev/null && echo "ERROR: obj/ still present"
```

### Step 5: Create Zip

```bash
cd /c/tmp/release
powershell Compress-Archive -Path 'spookys-automod-toolkit-v1.X.X' -DestinationPath 'spookys-automod-toolkit-v1.X.X.zip' -Force
```

## Verification Checklist

Before uploading:

- [ ] README.txt at root
- [ ] .claude/skills/ complete
- [ ] bin/Release/ directories present with DLLs
- [ ] NO bin/Debug/ directories
- [ ] NO obj/ directories
- [ ] NO tests/ directory
- [ ] NO .git/ directory
- [ ] NO root test files (*.csx, ConditionApiTest.*, temp/)
- [ ] CHANGELOG.md present and up to date
- [ ] Zip size: 20-50MB (with Release binaries)

## Size Expectations

**With pre-built binaries:**
- Compressed: ~20-50 MB
- Uncompressed: ~100-150 MB

**Without binaries (wrong):**
- Compressed: ~3-5 MB
- Uncompressed: ~6-10 MB

If zip is <10MB, you forgot to include bin/Release/ directories!

## Why This Approach?

**For End Users:**
- Unzip and immediately use - no build required
- `dotnet run --project src/SpookysAutomod.Cli` works immediately
- Claude Code skills auto-detected

**For Developers:**
- Complete source code included
- Can rebuild with `dotnet build`
- All documentation present

**For Claude Code:**
- Drop into projects directory
- Automatic skill detection
- Ready to use immediately
