# Release Structure Guide

This document defines the proper structure for GitHub releases.

## Critical Distinction

### Release Package = FOR END USERS
The release zip is a **ready-to-use distribution** for users who want to **USE the toolkit**. It contains pre-built binaries so users can unzip and immediately start creating mods. It should NOT contain files only useful for developing the toolkit itself.

### Source Code Repository = FOR TOOLKIT DEVELOPERS
The GitHub repository is for developers who want to **DEVELOP the toolkit**. It contains tests, Debug builds, IDE configurations, and other development artifacts.

**Release packages are for modders. Source code is for toolkit developers.**

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
    │   │   ├── *.cs                    # Source files
    │   │   ├── *.csproj                # Project files
    │   │   └── bin/Release/net8.0/     # ✓ Pre-built binaries
    │   └── ... (all modules)
    └── templates/                      # SKSE templates
```

## What to INCLUDE (End User Essentials)

### Essential Files
- README.txt (root level quick start)
- .claude/skills/ (all Claude Code skills)
- All source code (.cs, .csproj files)
- SpookysAutomod.sln (users can rebuild if needed)
- CHANGELOG.md, CLAUDE.md, CONTRIBUTING.md, README.md
- docs/ (complete user documentation)
- graphics/ (logo and images)
- templates/ (SKSE project templates)

### Pre-Built Binaries (CRITICAL)
- **bin/Release/net8.0/** directories in all src/ projects
- All Release-built DLLs and dependencies
- Users can run immediately: `dotnet run --project src/SpookysAutomod.Cli -- <command>`

## What to EXCLUDE (Toolkit Development Only)

### Development-Only Files
- **tests/** - Unit tests (toolkit developers only, not for modders)
- **bin/Debug/** - Debug builds (developers only)
- **obj/** - Intermediate build files (generated during compilation)
- **.vs/**, **.idea/**, **.vscode/** - IDE configurations
- **\*.user** files - User-specific project settings
- **.suo** files - Solution user options
- **Root test files** - test-\*.csx, ConditionApiTest.\*, temp/ folders

### Copyrighted Content
- **skyrim-script-headers/** - Bethesda copyright (users provide their own)
- **scripts/** - Compiled test scripts

### Git/Build Metadata
- **.git/** - Repository data
- **.github/** - CI/CD workflows
- **.papyrus/** - Compiler cache
- **RELEASE.md** - This documentation (for maintainers)

## README.txt Template

Keep it simple and focused on end users:

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
mkdir -p /c/tmp/release/spookys-automod-toolkit-v1.X.X/spookys-automod-toolkit

# Copy .claude/
cp -r .claude /c/tmp/release/spookys-automod-toolkit-v1.X.X/

# Create README.txt
cat > /c/tmp/release/spookys-automod-toolkit-v1.X.X/README.txt << 'EOF'
# Spooky's AutoMod Toolkit v1.X.X

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - The toolkit source code

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## Quick Start

See spookys-automod-toolkit/README.md for installation and usage instructions.
EOF

# Copy files (including bin/Release/, excluding development artifacts)
cd path/to/repository
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

### Step 3: Verify Structure

```bash
cd /c/tmp/release/spookys-automod-toolkit-v1.X.X/spookys-automod-toolkit

# ✓ Check bin/Release/ exists with DLLs
ls src/SpookysAutomod.Cli/bin/Release/net8.0/*.dll

# ✗ Verify excluded directories are gone
ls tests 2>/dev/null && echo "ERROR: tests/ present (remove it)"
ls bin/Debug 2>/dev/null && echo "ERROR: bin/Debug/ present (remove it)"
ls obj 2>/dev/null && echo "ERROR: obj/ present (remove it)"
ls temp 2>/dev/null && echo "ERROR: temp/ present (remove it)"

# ✓ Check size (should be 30-50MB uncompressed with binaries)
du -sh .
```

### Step 4: Create Zip

```bash
cd /c/tmp/release
powershell Compress-Archive -Path 'spookys-automod-toolkit-v1.X.X' -DestinationPath 'spookys-automod-toolkit-v1.X.X.zip' -Force
ls -lh *.zip
```

### Step 5: Upload to GitHub

```bash
cd path/to/repository
gh release create vX.Y.Z --title "vX.Y.Z - Title" --notes "Release notes..."
gh release upload vX.Y.Z /c/tmp/release/spookys-automod-toolkit-v1.X.X.zip
```

## Verification Checklist

Before uploading a release:

**MUST HAVE:**
- [ ] README.txt at root
- [ ] .claude/skills/ complete
- [ ] bin/Release/ directories with DLLs
- [ ] docs/ complete
- [ ] templates/ complete
- [ ] CHANGELOG.md up to date

**MUST NOT HAVE:**
- [ ] NO tests/ directory
- [ ] NO bin/Debug/ directories
- [ ] NO obj/ directories
- [ ] NO .vs/, .idea/, .vscode/ folders
- [ ] NO \*.user or .suo files
- [ ] NO .git/ directory
- [ ] NO skyrim-script-headers/
- [ ] NO root test files (test-\*.csx, ConditionApiTest.\*, temp/)
- [ ] NO RELEASE.md (this file)

## Size Expectations

**Correct size (with Release binaries only):**
- Compressed: 10-20 MB
- Uncompressed: 30-50 MB

**If you see these sizes, something is wrong:**
- <5 MB compressed = Missing bin/Release/ directories
- >50 MB compressed = Included tests/ or bin/Debug/

## Why This Approach?

### For End Users (Modders)
- Unzip and immediately use
- No build required
- No confusing test files
- Clean, professional package
- All documentation included

### For Toolkit Developers
- Clone the repository instead
- Full access to tests/
- Debug builds available
- IDE configurations present
- Can contribute improvements

**Release = Ready to use. Repository = Ready to develop.**
