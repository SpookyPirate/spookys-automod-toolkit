# Release Structure Guide

This document defines the proper structure for GitHub releases to ensure clean, professional distributions without development artifacts.

## Directory Structure

```
spookys-automod-toolkit-vX.Y.Z/
├── README.txt                          # Quick start guide for release
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
    ├── Directory.Build.props
    ├── README.md
    ├── SpookysAutomod.sln
    ├── docs/                           # User documentation
    │   ├── archive.md
    │   ├── audio.md
    │   ├── esp.md
    │   ├── llm-guide.md
    │   ├── llm-init-prompt.md
    │   ├── mcm.md
    │   ├── nif.md
    │   ├── papyrus.md
    │   ├── README.md
    │   └── skse.md
    ├── graphics/                       # Logos and images
    │   └── spookys-automod-toolkit-logo.png
    ├── src/                            # Source code
    │   ├── SpookysAutomod.Archive/
    │   ├── SpookysAutomod.Audio/
    │   ├── SpookysAutomod.Cli/
    │   ├── SpookysAutomod.Core/
    │   ├── SpookysAutomod.Esp/
    │   ├── SpookysAutomod.Mcm/
    │   ├── SpookysAutomod.Nif/
    │   ├── SpookysAutomod.Papyrus/
    │   └── SpookysAutomod.Skse/
    └── templates/                      # SKSE project templates
        └── skse/
            ├── basic/
            └── papyrus-native/
```

## What to INCLUDE

### Essential Files
- `.gitignore` - Shows users what to ignore
- `CHANGELOG.md` - Release history
- `CLAUDE.md` - Project architecture for Claude
- `CONTRIBUTING.md` - Contribution guidelines
- `README.md` - Main documentation
- `SpookysAutomod.sln` - Solution file

### Source Code
- All `.cs` files
- All `.csproj` files
- Directory.Build.props
- Complete `src/` hierarchy

### Documentation
- All `docs/*.md` files
- `graphics/` folder with logo

### Templates
- Complete `templates/` directory structure
- All SKSE templates (basic, papyrus-native)

### Claude Skills
- Complete `.claude/skills/` directory
- All skill.md files

### README.txt
- Root-level quick start guide
- Points users to main README.md
- Explains directory structure

## What to EXCLUDE

### Development Artifacts
- `tests/` - Unit tests (not needed by end users)
- `bin/` - Build outputs (users build themselves)
- `obj/` - Intermediate build files
- `.vs/` - Visual Studio cache
- `.idea/` - Rider cache
- `.vscode/` - VS Code cache
- `*.user` - User-specific project files

### Copyrighted Content
- `skyrim-script-headers/` - Bethesda copyright
- `scripts/` - Compiled test scripts

### Git Metadata
- `.git/` - Git repository data
- `.github/` - GitHub workflows (already in repo)

### Cache Files
- `.papyrus/` - Compiler cache
- `*.cache` files in obj/
- `.suo` files

## Release README.txt Template

```txt
# Spooky's AutoMod Toolkit vX.Y.Z

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - The toolkit source code and documentation

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## For Developers

1. Install .NET 8 SDK: https://dotnet.microsoft.com/download
2. Build the toolkit: `dotnet build`
3. Run commands: `dotnet run --project src/SpookysAutomod.Cli -- <module> <command>`

## Quick Start

See spookys-automod-toolkit/README.md for complete installation and usage instructions.

## What's New

See spookys-automod-toolkit/CHANGELOG.md for release notes.
```

## Creating a Release

### Using PowerShell Script

```powershell
# Set version
$version = "1.7.0"
$source = "C:\path\to\repository"
$releaseDir = "C:\tmp\release-build"
$releaseName = "spookys-automod-toolkit-v$version"

# Create structure
New-Item -ItemType Directory -Path "$releaseDir\$releaseName" -Force
New-Item -ItemType Directory -Path "$releaseDir\$releaseName\spookys-automod-toolkit" -Force

# Copy .claude/ skills
Copy-Item -Path "$source\.claude" -Destination "$releaseDir\$releaseName\.claude" -Recurse -Force

# Copy README.txt (create from template)
$readmeTxt = @"
# Spooky's AutoMod Toolkit v$version

## Directory Structure

This release contains:
- **.claude/** - Claude Code skills for AI-assisted modding
- **spookys-automod-toolkit/** - The toolkit source code and documentation

## For Claude Code Users

Place this entire folder in your projects directory. Claude Code will automatically
detect the .claude/skills/ and enable AI-assisted Skyrim modding commands.

## For Developers

1. Install .NET 8 SDK: https://dotnet.microsoft.com/download
2. Build the toolkit: ``dotnet build``
3. Run commands: ``dotnet run --project src/SpookysAutomod.Cli -- <module> <command>``

## Quick Start

See spookys-automod-toolkit/README.md for complete installation and usage instructions.

## What's New

See spookys-automod-toolkit/CHANGELOG.md for release notes.
"@
Set-Content -Path "$releaseDir\$releaseName\README.txt" -Value $readmeTxt

# Copy source files (excluding development artifacts)
$excludedDirs = @('.git', 'bin', 'obj', 'tests', '.vs', '.idea', '.vscode',
                  'skyrim-script-headers', 'scripts', '.papyrus')

Get-ChildItem -Path $source -Recurse -Force | Where-Object {
    $item = $_
    $excluded = $false

    # Check if item is in excluded directory
    foreach ($dir in $excludedDirs) {
        if ($item.FullName -match "\\$dir\\" -or $item.Name -eq $dir) {
            $excluded = $true
            break
        }
    }

    # Skip .claude (already copied separately)
    if ($item.FullName -match '\\.claude\\') {
        $excluded = $true
    }

    -not $excluded
} | ForEach-Object {
    $relativePath = $_.FullName.Substring($source.Length + 1)
    $targetPath = Join-Path "$releaseDir\$releaseName\spookys-automod-toolkit" $relativePath

    if ($_.PSIsContainer) {
        New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
    } else {
        $targetDir = Split-Path -Parent $targetPath
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        Copy-Item -Path $_.FullName -Destination $targetPath -Force
    }
}

# Create zip
Compress-Archive -Path "$releaseDir\$releaseName" -DestinationPath "$releaseDir\$releaseName.zip" -Force

Write-Host "Release created: $releaseDir\$releaseName.zip"
```

## Verification Checklist

Before uploading a release:

- [ ] README.txt exists at root level
- [ ] .claude/skills/ directory is complete
- [ ] All documentation in docs/ is present
- [ ] templates/ directory is complete
- [ ] src/ contains all source files
- [ ] NO bin/ or obj/ directories
- [ ] NO tests/ directory
- [ ] NO .git/ directory
- [ ] NO skyrim-script-headers/ directory
- [ ] NO scripts/ directory (compiled test scripts)
- [ ] CHANGELOG.md is present and up to date
- [ ] README.md is present
- [ ] Zip file size is reasonable (~5-20MB, not 140MB)

## Why This Structure?

**For End Users:**
- Clean, professional appearance
- No confusing test files
- All documentation included
- Ready to build and use

**For Developers:**
- Complete source code
- All templates for development
- Architecture documentation (CLAUDE.md)
- Contribution guidelines

**For Claude Code:**
- Skills directory in expected location
- Can be dropped directly into projects folder
- Automatic skill detection

## Size Expectations

A proper release should be:
- **Compressed:** ~5-20 MB (without build artifacts)
- **Uncompressed:** ~10-30 MB

If the zip is >40MB, you likely included bin/obj/tests directories.
