# {{PROJECT_NAME}}

**Author:** {{AUTHOR}}
**Version:** {{VERSION_MAJOR}}.{{VERSION_MINOR}}.{{VERSION_PATCH}}
**Description:** {{DESCRIPTION}}

## Build Requirements

- **CMake 3.24+**
- **MSVC Build Tools** (Visual Studio 2022 or Build Tools package)
- **CommonLibSSE-NG** (for Skyrim AE/SE plugin development)

## Setup Options

### Option 1: Using VCPKG (Recommended)

VCPKG automatically downloads and manages dependencies.

**Setup Steps:**

1. Install VCPKG globally:
   ```cmd
   git clone https://github.com/Microsoft/vcpkg.git C:\vcpkg
   cd C:\vcpkg
   .\bootstrap-vcpkg.bat
   ```

2. Install dependencies:
   ```cmd
   vcpkg install commonlibsse-ng:x64-windows-static
   ```

3. Integrate with CMake:
   ```cmd
   vcpkg integrate install
   ```

4. Build the project:
   ```cmd
   mkdir build
   cd build
   cmake .. -DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake
   cmake --build . --config Release
   ```

### Option 2: Manual Vendor Folder (No VCPKG)

If you can't use VCPKG, manually download dependencies.

**Setup Steps:**

1. Create `vendor/` folder in project root:
   ```cmd
   mkdir vendor
   ```

2. Download and extract these libraries into `vendor/`:
   - **CommonLibSSE-NG**: https://github.com/CharmedBaryon/CommonLibSSE-NG
     - Extract to: `vendor/CommonLibSSE-NG/`
   - **spdlog**: https://github.com/gabime/spdlog
     - Extract to: `vendor/spdlog/`
   - **xbyak**: https://github.com/herumi/xbyak
     - Extract to: `vendor/xbyak/`

3. Edit `CMakeLists.txt`:
   - Comment out lines 47-49 (VCPKG find_package)
   - Uncomment lines 57-67 (manual vendor setup)

4. Build:
   ```cmd
   mkdir build
   cd build
   cmake ..
   cmake --build . --config Release
   ```

## Quick Build (Windows CMD)

Use the provided `build.bat` script:

```cmd
REM For VCPKG users:
build.bat

REM For manual vendor setup:
build.bat --no-vcpkg

REM Clean build:
build.bat --clean

REM Debug build:
build.bat --debug
```

## Installation

1. After building, the DLL will be in `build/Release/{{PROJECT_NAME}}.dll`
2. Copy the DLL to: `<Skyrim>\Data\SKSE\Plugins\`
3. Launch Skyrim with SKSE

## Troubleshooting

### "Cannot open include file: 'RE/Skyrim.h'"

**Solution:** Make sure CommonLibSSE-NG is installed correctly. If using vendor folder, verify path is correct in CMakeLists.txt.

### "Cannot open include file: 'xbyak/xbyak.h'"

**Solution:** If using VCPKG, xbyak is included in CommonLibSSE-NG. If using vendor folder, extract xbyak to `vendor/xbyak/`

### "Incomplete type error with RE::TESObjectREFR"

**Solution:** This template uses `#include <RE/Skyrim.h>` in PCH.h which includes ALL headers. This error should not occur. Ensure PCH is being used correctly (CMake handles this automatically via `target_precompile_headers`).

### "undefined symbol: std::literals"

**Solution:** This template uses C++23. Ensure CMake is using the correct standard (set in CMakeLists.txt line 15).

### "CompatibleVersions not found"

**Solution:** The old SKSE API is deprecated. This template uses C++20 designated initializers for `PluginVersionData` with `.RuntimeCompatibility()`. Do NOT use the old `CompatibleVersions()` method.

### "error C2440: cannot convert 'RE::NiPointer<RE::TESObjectREFR>' to 'RE::NiPointer<T> &'"

**Solution:** Use `.get()` to get the raw pointer, then use `.As<RE::Actor>()` for safe casting:
```cpp
auto target = event->target.get();  // Get raw pointer
auto targetActor = target->As<RE::Actor>();  // Safe cast to Actor
```

### PCH Issues

**Solution:** This template configures PCH via CMake's `target_precompile_headers()`. Do NOT manually `#include "PCH.h"` in `.cpp` files - CMake handles it automatically. All source files implicitly include PCH.h first.

## Project Structure

```
{{PROJECT_NAME}}/
├── CMakeLists.txt      # Build configuration
├── vcpkg.json          # VCPKG dependencies
├── build.bat           # Quick build script
├── README.md           # This file
└── src/
    ├── PCH.h           # Precompiled header (all RE:: and SKSE:: headers)
    └── main.cpp        # Plugin entry point and implementation
```

## Modifying the Plugin

### Adding Event Handlers

See examples in `src/main.cpp`:
- `OnHitEventHandler` - Handles combat hits
- `OnEquipEventHandler` - Handles item equip/unequip

To add your own event handler:
1. Create a class that inherits from `RE::BSTEventSink<EventType>`
2. Implement `ProcessEvent()` method
3. Register it in `InitializeEventHandlers()`

### Looking Up Forms

```cpp
// By EditorID (string literal with sv suffix)
auto form = RE::TESForm::LookupByEditorID("IronSword"sv);

// By FormID (hex)
auto form = RE::TESForm::LookupByID(0x00012EB7);

// Typed lookup (automatically casts)
auto weapon = RE::TESForm::LookupByID<RE::TESObjectWEAP>(0x00012EB7);
```

### Accessing Player

```cpp
auto player = RE::PlayerCharacter::GetSingleton();
if (player) {
    SKSE::log::info("Player name: {}", player->GetName());
}
```

### Safe Actor Casting

```cpp
// From TESObjectREFR to Actor
auto refr = ...; // some RE::TESObjectREFR*
auto actor = refr->As<RE::Actor>();
if (actor) {
    // Now safe to use actor-specific methods
    SKSE::log::info("Actor health: {}", actor->GetActorValue(RE::ActorValue::kHealth));
}
```

### Working with NiPointer

```cpp
// NiPointer from events requires .get() first
auto target = event->target.get();  // RE::TESObjectREFR*
if (target) {
    auto actor = target->As<RE::Actor>();
}

// For direct form lookups, no .get() needed
auto player = RE::PlayerCharacter::GetSingleton();  // Already a raw pointer
```

## Resources

- **CommonLibSSE-NG Docs**: https://github.com/CharmedBaryon/CommonLibSSE-NG
- **SKSE Source**: https://github.com/ianpatt/skse64
- **Example Plugins**: https://github.com/topics/commonlibsse
- **VCPKG**: https://github.com/Microsoft/vcpkg

## License

[Add your license here]
