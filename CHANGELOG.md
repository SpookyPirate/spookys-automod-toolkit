# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),

and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Quest alias system** for follower tracking and dynamic NPC management
    - `esp add-alias` - Create reference aliases with optional scripts and flags
    - `esp attach-alias-script` - Attach scripts to existing aliases
    - `esp set-property --alias-target` - Set properties on alias scripts
    - Alias flags: Optional, AllowReuseInQuest, AllowReserved, Essential, etc.
- **Type-aware auto-fill** for script properties
    - `esp auto-fill` - Automatically fill properties by parsing PSC files
    - `esp auto-fill-all` - Bulk auto-fill all scripts in a mod
    - Searches Skyrim.esm by EditorID with type filtering
    - Prevents wrong matches (e.g., Location vs Keyword with similar names)
    - Supports 40+ Papyrus type → Mutagen type mappings
    - Array property support: `Keyword[] Property MyKeywords Auto`
    - Cached link cache for 5x performance improvement on repeated operations
- **Type inspection tools** for debugging
    - `esp debug-types` - Show Mutagen type structures with reflection
    - Supports pattern matching (e.g., `Quest*`, `QuestAlias`)
    - Displays properties, types, nullability, and critical notes
    - Essential for understanding Mutagen's type system
- **Dry-run mode** for all add commands
    - `--dry-run` flag previews changes without saving
    - Works with: add-weapon, add-armor, add-spell, add-perk, add-book, add-quest, add-global, add-npc
    - Useful for testing and validation workflows
- **Faction support**
    - `esp add-faction` - Create faction records
    - Configure flags: HiddenFromPC, TrackCrime, etc.
- **Enhanced analysis**
    - `esp analyze` - Detailed plugin analysis including aliases, scripts, and properties

### Changed

- Documentation significantly expanded with quest alias patterns
- LLM guide expanded with alias workflows and auto-fill usage
- **SKSE documentation clarified** - Removed misleading Visual Studio IDE requirement
    - Only CMake and MSVC Build Tools needed (no IDE required)
    - Added complete setup instructions to README
    - Clarified that LLMs can invoke CMake to build plugins end-to-end
    - SKSE moved from "limitations" to "fully supported" when tools installed

### Technical

- Services: AliasService, ScriptPropertyService, AutoFillService, BulkAutoFillService, TypeInspectionService, LinkCacheManager
- Builders: FactionBuilder, ScriptBuilder extended with WithObjectProperty() and WithArrayProperty()
- Auto-fill always loads Skyrim.esm for vanilla record lookups
- QuestFragmentAlias.Property.Object correctly references quest FormKey
- Link cache caching with 5-minute timeout for performance optimization
- Reflection-based Mutagen type introspection for debugging
- Regex-based PSC property parsing with type detection

## [1.4.1] - 2026-01-12

### Fixed

- **Critical:** Fixed PapyrusService.CompileAsync() parameter mismatch causing compilation errors
    - Was passing `optimize` (bool) to position 4, which expects `additionalImports` (List<string>?)
    - Now correctly passes `null` for additionalImports parameter
    - Prevented the toolkit from compiling at all

### Changed

- Separated `.claude` skills from source repository
    - Skills now packaged with releases for end users
    - Clean source repository for developers
    - Releases have proper structure for Claude Code auto-detection

### Added

- Release build scripts (PowerShell and Bash) for automated packaging
- Scripts documentation in `scripts/README.md`

## [1.4.0] - 2026-01-03

### Fixed

- **Critical:** Champollion decompiler wrapper using wrong argument (`-o` → `--psc`)
- Decompiled files now written to specified output directory
- Command status now accurately reflects success/failure
- Error messages now include actual Champollion output
- Added helpful suggestions for common decompilation errors

### Changed

- Enhanced error context propagation in `ChampollionWrapper.cs`
- Added `ParseDecompilerSuggestions()` method for better error messages

### Impact

- `papyrus decompile` command now fully functional
- All decompilation tests pass

## [1.3.0] - 2026-01-03

### Added

- LLM initialization prompt for quick AI assistant onboarding
- Documentation for rapid setup with Claude, ChatGPT, etc.

### Security

- Removed personal file paths from public documentation

## [1.2.0] - 2026-01-02

### Added

- Papyrus script headers infrastructure
- Support for SKSE and SkyUI headers
- Comprehensive Papyrus compilation support

### Fixed

- Papyrus compilation error reporting
- Documentation references to script header directories

## [1.1.0] - 2025-12-30

### Added

- Test suite infrastructure
- Archive list command implementation
- Test project for archive functionality

### Fixed

- JSON output formatting
- Null checks in CLI commands
- .gitignore matching for SpookysAutomod.Esp directory

## [1.0.0] - 2025-12-30

### Added

- Initial release
- ESP/ESL plugin creation and modification
- Papyrus script compilation and decompilation
- BSA/BA2 archive handling
- NIF mesh inspection
- MCM Helper configuration generation
- Audio file processing (FUZ/XWM/WAV)
- SKSE C++ plugin project scaffolding
- Claude Code skills for all modules
- Comprehensive documentation
- JSON output support for all commands
- Auto-downloading of external tools

### Features

- Create weapons, armor, spells, perks, books, quests, NPCs
- Compile and decompile Papyrus scripts
- Extract and create BSA archives
- Inspect 3D meshes and textures
- Generate MCM configuration menus
- Process game audio files
- Generate SKSE plugin projects

[unreleased]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.4.1...HEAD

[1.4.1]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.4.0...v1.4.1

[1.4.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.3.0...v1.4.0

[1.3.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.2.0...v1.3.0

[1.2.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.1.0...v1.2.0

[1.1.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.0.0...v1.1.0

[1.0.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/releases/tag/v1.0.0