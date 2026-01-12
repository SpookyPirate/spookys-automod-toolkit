# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
