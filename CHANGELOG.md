# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),

and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.7.0] - 2026-01-29

### Added

- **Record Viewing and Override System** - Comprehensive tools for inspecting and modifying existing records
    - `esp view-record` - View detailed information about any record by EditorID or FormID
        - Supports 15 record types: Spell, Weapon, Armor, Quest, NPC, Perk, Faction, Book, MiscItem, Global, LeveledItem, FormList, Outfit, Location, EncounterZone
        - Extracts type-specific properties (spell effects, weapon damage, armor ratings, etc.)
        - Optional `--include-raw` flag for reflection-based property extraction
        - Full JSON output support for AI parsing
    - `esp create-override` - Create override patches for existing records
        - Automatically adds source plugin as master reference
        - Preserves all record data via DeepCopy
        - Creates patches that load after source in load order
        - Supports all major record types
    - `esp find-record` - Search for records across one or multiple plugins
        - Pattern matching on EditorID and Name fields
        - Filter by record type for faster searches
        - Search single plugin or all plugins in data folder
        - Returns plugin name, FormKey, EditorID, and record type
    - `esp batch-override` - Create overrides for multiple records at once
        - Batch by search pattern or explicit EditorID list
        - Filter by record type
        - All overrides in single patch plugin
        - Reports number of records modified
    - `esp compare-record` - Compare two versions of the same record
        - Side-by-side property comparison
        - Highlights differences between original and modified
        - Useful for understanding what patches change
        - Supports EditorID or FormID lookup
    - `esp conflicts` - Detect load order conflicts
        - Scans all plugins in data folder
        - Shows which plugins modify the same record
        - Reports load order positions
        - Identifies winning override (last in load order)

- **Condition Management System** - View, add, and remove conditions on records
    - `esp list-conditions` - View all conditions on a record (Perk, Package, IdleAnimation, MagicEffect)
        - Shows condition function, comparison value, parameters, flags
        - Numbered output for easy reference when removing
        - Full JSON support for AI parsing
    - `esp add-condition` - Add conditions to records
        - Supports Perk, Package, IdleAnimation, MagicEffect record types
        - Common condition functions: GetLevel, IsInCombat, GetActorValue, etc.
        - Creates override patch automatically
        - Hardcoded comparison value of 1.0 (suitable for most boolean checks)
    - `esp remove-condition` - Remove specific conditions by index
        - Remove single or multiple conditions (comma-separated indices)
        - Preserves all other record properties
        - Creates clean override patch
    - **Important:** Conditions supported on Perk, Package, IdleAnimation, MagicEffect only
    - Conditions NOT supported on Spell, Weapon, Armor (Mutagen API limitation)

- **Script Property Management**
    - `esp set-property` - Manually set script properties on quest or alias scripts
        - Set properties on quest scripts or alias scripts (via `--alias-target`)
        - Supports 6 property types: `object`, `alias`, `int`, `float`, `bool`, `string`
        - Object properties use FormKey format: `Plugin.esp|0xFormID`
        - Alias properties reference aliases within same quest
        - Full JSON output support
        - Complements `esp auto-fill` for manual property overrides
        - Ported from unmerged PR #1, resolves user-reported documentation issue

### Changed

- **Enhanced Error Messages** - All new commands provide helpful suggestions on failure
- **Improved JSON Output** - Consistent Result<T> pattern across all operations
- **Better FormKey Handling** - Support for both short and long FormKey formats

### Fixed

- **SKSE Template Modernization** - Completely updated SKSE plugin templates for CommonLibSSE-NG
    - Fixed deprecated `CompatibleVersions` API → Now uses `.RuntimeCompatibility(Independent)`
    - Added proper PCH configuration via CMake `target_precompile_headers()`
    - Fixed incomplete type errors with `RE::TESObjectREFR` by including `<RE/Skyrim.h>` in PCH
    - Fixed `NiPointer` conversion errors with correct `.get()` and `.As<T>()` usage
    - Fixed `LookupForm` API misuse → Now uses `LookupByEditorID()` for EditorID strings
    - Added comprehensive README with VCPKG and manual vendor/ setup instructions
    - Added `build.bat` script for easy Windows CMD building
    - Resolves 7 major user-reported compilation issues

### Developer Notes

- Added 8 new model classes in `SpookysAutomod.Core.Models`
- Extended PluginService with 6 major methods (~1,640 lines)
- Added 6 new CLI commands to EspCommands (~910 lines)
- Total new code: ~3,500 lines
- No new dependencies required

### Known Limitations

- Condition support temporarily disabled pending Mutagen API verification
- Conditions work on Perks but not Spells/Weapons/Armor in current Mutagen version
- Future versions will add full condition manipulation support

## [1.6.0] - 2026-01-20

### Added

- **LeveledItem support** for random loot and equipment lists
    - `esp add-leveled-item` - Create leveled item lists with entries and chance-none
    - Presets: low-treasure, medium-treasure, high-treasure, guaranteed-loot
    - Support for entry management with level and count parameters
- **FormList support** for script property collections
    - `esp add-form-list` - Create form lists for use in scripts and conditions
    - Accepts FormKeys or EditorIDs
- **EncounterZone support** for level scaling areas
    - `esp add-encounter-zone` - Create encounter zones with min/max levels
    - Presets: low-level (1-10), mid-level (10-30), high-level (30-50), scaling (1-unlimited)
    - Flags: never-resets, match-pc-below-min, disable-combat-boundary
- **Location support** for quest locations and fast travel
    - `esp add-location` - Create location records with keywords and parent locations
    - Presets: inn, city, dungeon, dwelling
    - Support for location keywords (LocTypeInn, LocTypeCity, etc.)
- **Outfit support** for NPC equipment sets
    - `esp add-outfit` - Create outfit records for NPC clothing/armor assignments
    - Presets: guard (iron armor + sword/shield), farmer (clothes), mage (robes), thief (leather)
    - Support for multiple armor and weapon items

### Fixed

- AutoFillService now includes Outfit type mapping for auto-fill functionality

## [1.5.0] - 2026-01-20

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
    - **Complete array property support** using `ScriptObjectListProperty`
        - Auto-fill creates proper multi-element arrays for PSC array properties
        - ScriptBuilder.WithArrayProperty() supports multiple FormKeys
        - Currently fills arrays with single matching element (first match)
        - Future: Pattern matching and explicit multi-element lists
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
- Array properties use Mutagen's ScriptObjectListProperty with ExtendedList<ScriptObjectProperty>
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

[unreleased]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.5.0...HEAD

[1.5.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.4.1...v1.5.0

[1.4.1]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.4.0...v1.4.1

[1.4.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.3.0...v1.4.0

[1.3.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.2.0...v1.3.0

[1.2.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.1.0...v1.2.0

[1.1.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/compare/v1.0.0...v1.1.0

[1.0.0]: https://github.com/SpookyPirate/spookys-automod-toolkit/releases/tag/v1.0.0