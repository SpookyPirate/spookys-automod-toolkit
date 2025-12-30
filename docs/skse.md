# SKSE Module Reference

The SKSE module handles creation and management of SKSE (Skyrim Script Extender) C++ plugin projects.

## Overview

SKSE plugins are DLL files that extend Skyrim's functionality at a native level. This module generates project scaffolding using **CommonLibSSE-NG**, which supports Skyrim SE, AE, GOG, and VR from a single codebase.

**Note:** Building SKSE plugins requires:
- Visual Studio 2022 (or Build Tools)
- CMake 3.21+
- vcpkg (auto-bootstrapped)

## Commands

### templates

List available SKSE project templates.

```bash
skse templates
```

**Available Templates:**

| Template | Description |
|----------|-------------|
| `basic` | Minimal SKSE plugin with logging |
| `papyrus-native` | Plugin with Papyrus native function support |

**Example:**
```bash
skse templates
```

---

### create

Create a new SKSE plugin project.

```bash
skse create <name> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `name` | Project name |

**Options:**
| Option | Default | Description |
|--------|---------|-------------|
| `--template` | `basic` | Template to use |
| `--output` | `.` | Output directory |
| `--author` | `Unknown` | Author name |
| `--description` | - | Project description |

**Examples:**
```bash
# Basic plugin
skse create "MyPlugin" --output "./"

# Papyrus native functions plugin
skse create "MyNativePlugin" --template papyrus-native --author "YourName" --output "./"
```

**Generated Structure:**
```
MyPlugin/
  CMakeLists.txt
  vcpkg.json
  src/
    main.cpp
    plugin.cpp
    plugin.h
  cmake/
    CommonLibSSE.cmake
```

---

### info

Get information about an SKSE project.

```bash
skse info <path>
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `path` | Project directory (default: current) |

**Output includes:**
- Project name
- Author
- Version
- Template used
- Description
- Target Skyrim versions
- Papyrus functions (if any)

**Example:**
```bash
skse info "./MyPlugin"
```

---

### add-function

Add a Papyrus native function to a project.

```bash
skse add-function <project> --name <name> [options]
```

**Arguments:**
| Argument | Description |
|----------|-------------|
| `project` | Project directory |

**Required Options:**
| Option | Description |
|--------|-------------|
| `--name` | Function name |

**Optional:**
| Option | Default | Description |
|--------|---------|-------------|
| `--return` | `void` | Return type |
| `--param` | - | Parameters (format: `type:name`, can repeat) |

**Papyrus Types:**
| Papyrus | C++ |
|---------|-----|
| Int | int |
| Float | float |
| Bool | bool |
| String | std::string |
| Actor | RE::Actor* |
| ObjectReference | RE::TESObjectREFR* |
| Form | RE::TESForm* |

**Examples:**
```bash
# Simple function
skse add-function "./MyPlugin" --name "GetPluginVersion" --return "Int"

# Function with parameters
skse add-function "./MyPlugin" --name "SetActorSpeed" --return "void" --param "Actor:target" --param "Float:speed"

# Function returning Actor
skse add-function "./MyPlugin" --name "GetNearestActor" --return "Actor" --param "ObjectReference:origin" --param "Float:radius"
```

---

## Building Projects

After creating a project:

```bash
# 1. Navigate to project
cd MyPlugin

# 2. Configure with CMake
cmake -B build -S .

# 3. Build
cmake --build build --config Release

# Output: build/Release/MyPlugin.dll
```

### Build Requirements

| Tool | Purpose | Installation |
|------|---------|--------------|
| Visual Studio 2022 | C++ compiler | [Download](https://visualstudio.microsoft.com/) |
| CMake 3.21+ | Build system | [Download](https://cmake.org/download/) |
| vcpkg | Dependencies | Auto-bootstrapped by project |

---

## Template Details

### basic

Minimal plugin with:
- SKSE plugin info
- Logging setup
- OnInit hook

```cpp
extern "C" DLLEXPORT bool SKSEAPI SKSEPlugin_Load(const SKSE::LoadInterface* a_skse) {
    SKSE::Init(a_skse);
    // Your code here
    return true;
}
```

### papyrus-native

Includes everything in `basic` plus:
- Papyrus native function registration
- Script interface
- Example function

```cpp
// Register functions
bool RegisterFunctions(RE::BSScript::IVirtualMachine* vm) {
    vm->RegisterFunction("MyFunction", "MyScript", MyFunction);
    return true;
}

// Example native function
int MyFunction(RE::StaticFunctionTag*) {
    return 42;
}
```

Papyrus usage:
```papyrus
Int value = MyScript.MyFunction()
```

---

## CommonLibSSE-NG

This toolkit uses **CommonLibSSE-NG** (Next Generation), which provides:

- **Multi-version support**: Single DLL works on SE, AE, GOG, VR
- **Address independence**: No hardcoded addresses
- **Modern C++**: Uses C++20 features
- **Complete API**: Covers most game functions

### Supported Skyrim Versions

| Version | Support |
|---------|---------|
| Skyrim SE 1.5.x | Full |
| Skyrim SE 1.6.x (AE) | Full |
| Skyrim GOG | Full |
| Skyrim VR | Partial |

---

## Project Configuration

Projects store configuration in `skse_config.json`:

```json
{
  "name": "MyPlugin",
  "author": "YourName",
  "version": "1.0.0",
  "template": "papyrus-native",
  "description": "My SKSE Plugin",
  "targetVersions": ["SE", "AE"],
  "papyrusFunctions": [
    {
      "name": "GetActorSpeed",
      "returnType": "Float",
      "parameters": [
        { "type": "Actor", "name": "target" }
      ]
    }
  ]
}
```

---

## Limitations

This module can:
- Generate project scaffolding
- Add Papyrus native function stubs
- Manage project configuration

This module cannot:
- Write custom C++ logic
- Compile projects (requires local build tools)
- Debug plugins

For advanced SKSE development:
- [CommonLibSSE-NG Wiki](https://github.com/CharmedBaryon/CommonLibSSE-NG/wiki)
- [SKSE Plugin Development Guide](https://www.creationkit.com/index.php?title=Category:SKSE)

---

## JSON Output

All commands support `--json` for machine-readable output:

```bash
skse info "./MyPlugin" --json
```

**Success response:**
```json
{
  "success": true,
  "result": {
    "name": "MyPlugin",
    "author": "YourName",
    "version": "1.0.0",
    "template": "papyrus-native",
    "description": "My SKSE Plugin",
    "targetVersions": ["SE", "AE"],
    "papyrusFunctions": [
      {
        "name": "GetActorSpeed",
        "returnType": "Float",
        "parameters": [
          { "type": "Actor", "name": "target" }
        ]
      }
    ]
  }
}
```
