# Release Structure Guide

This document defines the proper structure for GitHub releases.

## Critical Distinction

### Release Package = FOR END USERS (Modders)
The release zip is a **ready-to-use runtime distribution** for users who want to **USE the toolkit** to create Skyrim mods. It contains:
- ✅ Pre-built binaries (bin/Release/net8.0/ with 78+ DLLs)
- ✅ User documentation (docs/, README.md, CHANGELOG.md)
- ✅ Claude Code skills (.claude/skills/)
- ✅ Templates and tools
- ❌ NO source code (.cs files, .csproj files, .sln)
- ❌ NO development documentation (CLAUDE.md, CONTRIBUTING.md)
- ❌ NO tests, Debug builds, or IDE configurations

Users can unzip and immediately start creating mods with Claude Code.

### Source Code Repository = FOR TOOLKIT DEVELOPERS
The GitHub repository is for developers who want to **DEVELOP the toolkit** itself. It contains:
- ✅ All source code (.cs, .csproj, .sln)
- ✅ Development documentation (CLAUDE.md, CONTRIBUTING.md)
- ✅ Tests, Debug builds, IDE configurations
- ✅ Research and tracking documentation
- ✅ Graphics and branding files

Developers clone the repository to build, test, and contribute improvements.

**Release packages are for modders. Source code is for toolkit developers.**

## Directory Structure

```
spookys-automod-toolkit-vX.Y.Z/
├── README.txt                          # Quick start guide
├── CHANGELOG.md                        # Release notes at root level
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
    ├── README.md
    ├── skyrim-script-headers/          # EMPTY folder - users add their own headers here
    ├── docs/                           # User documentation (10 files)
    ├── src/                            # Pre-built binaries ONLY (no source code)
    │   ├── SpookysAutomod.Archive/
    │   │   └── bin/Release/net8.0/     # ✓ Pre-built DLLs
    │   ├── SpookysAutomod.Audio/
    │   │   └── bin/Release/net8.0/
    │   ├── SpookysAutomod.Cli/
    │   │   └── bin/Release/net8.0/
    │   ├── SpookysAutomod.Core/
    │   │   └── bin/Release/net8.0/
    │   ├── SpookysAutomod.Esp/
    │   │   └── bin/Release/net8.0/
    │   ├── SpookysAutomod.Mcm/
    │   │   └── bin/Release/net8.0/
    │   ├── SpookysAutomod.Nif/
    │   │   └── bin/Release/net8.0/
    │   ├── SpookysAutomod.Papyrus/
    │   │   └── bin/Release/net8.0/
    │   └── SpookysAutomod.Skse/
    │       └── bin/Release/net8.0/
    ├── templates/                      # SKSE project templates
    └── tools/                          # External tool configurations
        ├── bsarch/
        ├── champollion/
        └── papyrus-compiler/
```

## What to INCLUDE (End User Essentials)

### Essential Files
- README.txt (root level quick start)
- CHANGELOG.md (at root level - easy to find release notes)
- .claude/skills/ (all Claude Code skills for AI-assisted modding)
- README.md (in toolkit folder - detailed documentation)
- docs/ (complete user documentation - 10 files)
- templates/ (SKSE project templates)
- tools/ (external tool configurations)
- **skyrim-script-headers/** - EMPTY folder (placeholder for users to add their own headers)

### Pre-Built Binaries (CRITICAL)
- **bin/Release/net8.0/** directories in all src/ projects (78+ DLLs)
- All Release-built DLLs and dependencies
- Users can run immediately: `dotnet run --project src/SpookysAutomod.Cli -- <command>`
- No compilation required - ready to use

### Important: Skyrim Script Headers
The **skyrim-script-headers/** folder should be EMPTY in the release. This folder is a placeholder for users to add their own Bethesda script headers (which are copyrighted and cannot be distributed). Users obtain headers from:
- Creation Kit installation
- Skyrim Special Edition installation
- SKSE source distribution

DO NOT copy the populated skyrim-script-headers/ folder from your development environment.

## What to EXCLUDE (Toolkit Development Only)

### Source Code and Project Files (Developers Only)
- **\*.cs** - All C# source files (toolkit developers clone repository instead)
- **\*.csproj** - All project files (9 files)
- **SpookysAutomod.sln** - Solution file (developers only)
- **CLAUDE.md** - Toolkit development guide (for contributors, not end users)
- **CONTRIBUTING.md** - Contribution guidelines (for developers)

### Development-Only Files
- **tests/** - Unit tests (toolkit developers only, not for modders)
- **bin/Debug/** - Debug builds (developers only)
- **obj/** - Intermediate build files (generated during compilation)
- **.vs/**, **.idea/**, **.vscode/** - IDE configurations
- **\*.user** files - User-specific project settings
- **.suo** files - Solution user options
- **Root test files** - test-\*.csx, ConditionApiTest.\*, temp/ folders
- **graphics/** - Logo files (not needed for functionality)
- **research/** - Development documentation (7 docs, for contributors only)

### Copyrighted Content (CRITICAL - DO NOT INCLUDE)
- **skyrim-script-headers/\*.psc** - Bethesda copyrighted script headers
  - DO NOT copy populated skyrim-script-headers/ from dev environment
  - Release should contain EMPTY skyrim-script-headers/ folder only
  - Users provide their own headers from Creation Kit/SKSE
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
- **spookys-automod-toolkit/** - Pre-built toolkit binaries and documentation

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## Quick Start

1. Ensure .NET 8 Runtime is installed
2. See spookys-automod-toolkit/README.md for installation and usage instructions
3. Run commands: `dotnet run --project spookys-automod-toolkit/src/SpookysAutomod.Cli -- <command>`

## For Toolkit Developers

This release contains pre-built binaries only. To develop the toolkit:
- Clone the repository: https://github.com/SpookyPirate/spookys-automod-toolkit
- See CLAUDE.md for development guidelines
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

# Copy CHANGELOG.md to root level
cp CHANGELOG.md /c/tmp/release/spookys-automod-toolkit-v1.X.X/

# Create README.txt
cat > /c/tmp/release/spookys-automod-toolkit-v1.X.X/README.txt << 'EOF'
# Spooky's AutoMod Toolkit v1.X.X

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - Pre-built toolkit binaries and documentation

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## Quick Start

1. Ensure .NET 8 Runtime is installed
2. See spookys-automod-toolkit/README.md for installation and usage instructions
3. Run commands: `dotnet run --project spookys-automod-toolkit/src/SpookysAutomod.Cli -- <command>`

## For Toolkit Developers

This release contains pre-built binaries only. To develop the toolkit:
- Clone the repository: https://github.com/SpookyPirate/spookys-automod-toolkit
- See CLAUDE.md for development guidelines
EOF

# Copy files (pre-built binaries only, NO source code)
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
    --exclude='graphics' \
    --exclude='research' \
    --exclude='*.cs' \
    --exclude='*.csproj' \
    --exclude='*.sln' \
    --exclude='*.csx' \
    --exclude='ConditionApiTest.*' \
    --exclude='CHANGELOG.md' \
    --exclude='CLAUDE.md' \
    --exclude='CONTRIBUTING.md' \
    --exclude='RELEASE.md' \
    -cf - . | tar -xf - -C /c/tmp/release/spookys-automod-toolkit-v1.X.X/spookys-automod-toolkit

# Cleanup: Remove any .cs/.csproj files that slipped through
cd /c/tmp/release/spookys-automod-toolkit-v1.X.X/spookys-automod-toolkit
find . -name "*.cs" -delete
find . -name "*.csproj" -delete
find . -name "*.sln" -delete
find . -type d -name Debug -exec rm -rf {} + 2>/dev/null
find . -type d -name obj -exec rm -rf {} + 2>/dev/null

# Create EMPTY skyrim-script-headers folder (placeholder for users)
mkdir -p skyrim-script-headers

# Verify skyrim-script-headers is empty (critical - no Bethesda copyright violations)
if [ -n "$(ls -A skyrim-script-headers 2>/dev/null)" ]; then
    echo "ERROR: skyrim-script-headers/ is not empty! Remove all files."
    exit 1
fi
```

### Step 3: Verify Structure

```bash
cd /c/tmp/release/spookys-automod-toolkit-v1.X.X

# ✓ Check CHANGELOG.md at root
ls CHANGELOG.md || echo "ERROR: CHANGELOG.md missing from root"

cd spookys-automod-toolkit

# ✓ Check bin/Release/ exists with DLLs
ls src/SpookysAutomod.Cli/bin/Release/net8.0/*.dll

# ✓ Verify skyrim-script-headers exists and is EMPTY (critical!)
ls -d skyrim-script-headers || echo "ERROR: skyrim-script-headers/ folder missing"
if [ -n "$(ls -A skyrim-script-headers 2>/dev/null)" ]; then
    echo "ERROR: skyrim-script-headers/ contains files (must be EMPTY)"
    ls -la skyrim-script-headers
fi

# ✗ Verify NO source code or project files
find . -name "*.cs" | head -5 && echo "ERROR: .cs files present (remove them)"
find . -name "*.csproj" | head -5 && echo "ERROR: .csproj files present (remove them)"
find . -name "*.sln" && echo "ERROR: .sln file present (remove it)"

# ✗ Verify excluded directories are gone
ls tests 2>/dev/null && echo "ERROR: tests/ present (remove it)"
ls bin/Debug 2>/dev/null && echo "ERROR: bin/Debug/ present (remove it)"
ls obj 2>/dev/null && echo "ERROR: obj/ present (remove it)"
ls temp 2>/dev/null && echo "ERROR: temp/ present (remove it)"
ls graphics 2>/dev/null && echo "ERROR: graphics/ present (remove it)"
ls research 2>/dev/null && echo "ERROR: research/ present (remove it)"

# ✗ Verify development docs are gone
ls CLAUDE.md 2>/dev/null && echo "ERROR: CLAUDE.md present (remove it)"
ls CONTRIBUTING.md 2>/dev/null && echo "ERROR: CONTRIBUTING.md present (remove it)"

# ✓ Check size (should be 35-40MB uncompressed with binaries only)
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
- [ ] CHANGELOG.md at root (not in toolkit folder)
- [ ] .claude/skills/ complete (7 modules)
- [ ] bin/Release/ directories with DLLs (9 modules)
- [ ] docs/ complete (10 user docs only)
- [ ] templates/ complete
- [ ] tools/ present
- [ ] README.md in toolkit folder
- [ ] skyrim-script-headers/ folder exists and is EMPTY

**MUST NOT HAVE (Source Code):**
- [ ] NO \*.cs files (source code - developers clone repository)
- [ ] NO \*.csproj files (project files)
- [ ] NO \*.sln file (solution file)
- [ ] NO CLAUDE.md (toolkit development guide)
- [ ] NO CONTRIBUTING.md (contribution guidelines)

**MUST NOT HAVE (Development Files):**
- [ ] NO tests/ directory
- [ ] NO bin/Debug/ directories
- [ ] NO obj/ directories
- [ ] NO .vs/, .idea/, .vscode/ folders
- [ ] NO \*.user or .suo files
- [ ] NO .git/ directory
- [ ] NO skyrim-script-headers/
- [ ] NO root test files (test-\*.csx, ConditionApiTest.\*, temp/)
- [ ] NO graphics/ directory
- [ ] NO research/ directory
- [ ] NO RELEASE.md (this file)

## Size Expectations

**Correct size (pre-built binaries only, NO source code):**
- Compressed: 10-15 MB
- Uncompressed: 35-40 MB (v1.8.0 = 38MB)

**If you see these sizes, something is wrong:**
- <30 MB uncompressed = Missing bin/Release/ directories or DLLs
- >45 MB uncompressed = Included source code (.cs files) or tests/
- >50 MB uncompressed = Included bin/Debug/ or multiple build configurations

## Why This Approach?

### For End Users (Modders)
- Unzip and immediately use - no build required
- Pre-built binaries ready to run
- No confusing source code or project files
- Clean, professional package (38MB)
- All documentation included
- Works with Claude Code out of the box

### For Toolkit Developers
- Clone the repository instead for full source code
- Full access to tests/ and development tools
- Debug builds available for debugging
- IDE configurations present (.vs/, .vscode/)
- CLAUDE.md and CONTRIBUTING.md for guidelines
- Can contribute improvements and build from source

**Key Philosophy:**
- **Release = Ready to use** - End users get binaries, docs, templates
- **Repository = Ready to develop** - Developers get source code, tests, build tools

**This separation ensures:**
1. End users aren't overwhelmed by 57+ .cs files they'll never need
2. Developers have full access to source by cloning the repository
3. Releases are focused, clean, and professional
4. File size stays reasonable (38MB vs 40MB+ with source)
