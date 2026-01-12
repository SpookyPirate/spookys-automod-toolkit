# Release Scripts

Scripts for building release packages with proper directory structure.

## Directory Structure

Releases have `.claude` skills at the root level, separate from the toolkit code:

```
spookys-automod-toolkit-v1.x.x/
├── .claude/                          # Claude Code skills (for end users)
│   └── skills/
│       ├── skyrim-esp/
│       ├── skyrim-papyrus/
│       └── ...
└── spookys-automod-toolkit/          # Toolkit source code
    ├── src/
    ├── docs/
    ├── README.md
    └── ...
```

This structure allows Claude Code to automatically detect skills when users place the release folder in their projects directory.

## Prerequisites

### 1. Store `.claude` Skills Externally

The `.claude` folder should NOT be in the git repository. Store it alongside the repo:

```
C:\Users\spook\Desktop\Projects\3. Development\skyrim-mods\
├── spookys-automod-toolkit\          # Git repository
└── toolkit-release-assets\
    └── .claude\                       # Skills for releases
        └── skills\
            ├── skyrim-esp\
            ├── skyrim-papyrus\
            └── ...
```

### 2. Build Tools

- **Windows:** PowerShell 5.0+
- **Linux/Mac:** Bash, `rsync`, `zip`
- **.NET SDK 8.0** (for building)

## Building a Release

### Windows (PowerShell)

```powershell
cd spookys-automod-toolkit
.\scripts\build-release.ps1 -Version "1.4.1"
```

**Custom paths:**
```powershell
.\scripts\build-release.ps1 `
    -Version "1.4.1" `
    -ClaudeSkillsPath "C:\path\to\.claude" `
    -OutputDir "C:\releases"
```

### Linux/Mac (Bash)

```bash
cd spookys-automod-toolkit
chmod +x scripts/build-release.sh
./scripts/build-release.sh 1.4.1
```

**Custom paths:**
```bash
./scripts/build-release.sh 1.4.1 /path/to/.claude /output/dir
```

## What the Scripts Do

1. **Build** the toolkit in Release configuration
2. **Copy** toolkit files to temp directory (excludes build artifacts, git, etc.)
3. **Copy** `.claude` skills to release root
4. **Create** release README.txt
5. **Zip** everything with proper structure
6. **Output** to `../releases/spookys-automod-toolkit-v{version}.zip`

## Output

The script creates:
```
../releases/spookys-automod-toolkit-v1.4.1.zip
```

When extracted:
```
spookys-automod-toolkit-v1.4.1/
├── README.txt                        # Release instructions
├── .claude/                          # Skills at root level
└── spookys-automod-toolkit/          # Toolkit code
```

## GitHub Releases

After building the release package:

1. **Create GitHub Release**
   ```bash
   gh release create v1.4.1 \
       ../releases/spookys-automod-toolkit-v1.4.1.zip \
       --title "v1.4.1 - Release Title" \
       --notes "Release notes here"
   ```

2. **Or upload manually** at https://github.com/SpookyPirate/spookys-automod-toolkit/releases/new

## Excluding `.claude` from Repository

The `.claude` folder should be gitignored:

```bash
# Already in .gitignore
.claude/
```

**Why?**
- Skills are usage documentation, not source code
- Keeps repository clean for developers
- Packaged separately for releases
- Can be updated independently

## Release Checklist

- [ ] Update `CHANGELOG.md` with new version
- [ ] Update version in README.md
- [ ] Commit all changes
- [ ] Tag release: `git tag v1.4.1`
- [ ] Push tags: `git push origin v1.4.1`
- [ ] Build release package: `.\scripts\build-release.ps1 -Version "1.4.1"`
- [ ] Test extracted release
- [ ] Create GitHub Release with zip
- [ ] Update download links in README (if any)
