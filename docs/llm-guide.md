# LLM Guide for Spookys AutoMod Toolkit

This guide is specifically designed for LLMs to understand how to use the toolkit effectively for autonomous Skyrim mod creation.

## Core Principles

1. **Always use --json flag** for parsing command output
2. **Check success status** before proceeding with dependent operations
3. **Use suggestions** from error responses to fix issues
4. **Chain commands** to build complete mods

## Workflow Patterns

### Pattern 1: Create a Complete Mod

```bash
# Step 1: Create the plugin
spookys-automod esp create "MyQuestMod" --light --author "LLM" --json

# Step 2: Add a main quest
spookys-automod esp add-quest "MyQuestMod.esp" "MQM_MainQuest" --name "The Main Quest" --start-enabled --json

# Step 3: Add a configuration global
spookys-automod esp add-global "MyQuestMod.esp" "MQM_Enabled" --type int --value 1 --json

# Step 4: Generate the quest script
spookys-automod papyrus generate --name "MQM_MainQuestScript" --extends "Quest" --output "./Scripts/Source" --json

# Step 5: Compile the script
spookys-automod papyrus compile "./Scripts/Source" --output "./Scripts" --headers "/path/to/skyrim/Data/Scripts/Source" --json

# Step 6: Attach script to quest
spookys-automod esp attach-script "MyQuestMod.esp" --quest "MQM_MainQuest" --script "MQM_MainQuestScript" --json

# Step 7: Generate SEQ file for start-enabled quest
spookys-automod esp generate-seq "MyQuestMod.esp" --output "./" --json

# Step 8: Create MCM configuration
spookys-automod mcm create "MyQuestMod" "My Quest Mod" --output "./MCM/config.json" --json
spookys-automod mcm add-toggle "./MCM/config.json" "bEnabled" "Enable Mod" --help-text "Toggle the mod on/off" --json
```

### Pattern 2: Create an SKSE Plugin with Native Functions

```bash
# Step 1: Create SKSE project
spookys-automod skse create "MyNativePlugin" --template papyrus-native --author "LLM" --output "./" --json

# Step 2: Add custom Papyrus functions
spookys-automod skse add-function "./MyNativePlugin" --name "GetActorSpeed" --return "float" --param "Actor:target" --json
spookys-automod skse add-function "./MyNativePlugin" --name "SetActorSpeed" --return "void" --param "Actor:target" --param "float:speed" --json

# Step 3: Build instructions are printed - user must have CMake and MSVC installed
# cmake -B build -S .
# cmake --build build --config Release
```

### Pattern 3: Analyze and Modify Existing Mod

```bash
# Step 1: Get plugin info
spookys-automod esp info "ExistingMod.esp" --json

# Step 2: Decompile scripts to understand behavior
spookys-automod papyrus decompile "./Scripts/ExistingScript.pex" --output "./Decompiled" --json

# Step 3: Inspect NIF meshes
spookys-automod nif info "./Meshes/weapon.nif" --json
spookys-automod nif textures "./Meshes/weapon.nif" --json

# Step 4: List archive contents
spookys-automod archive list "ExistingMod.bsa" --json
```

## JSON Response Handling

### Success Response

```json
{
  "success": true,
  "result": {
    // Command-specific data
  }
}
```

### Error Response

```json
{
  "success": false,
  "error": "Error message",
  "errorContext": "Additional context about the error",
  "suggestions": [
    "Suggested fix 1",
    "Suggested fix 2"
  ]
}
```

### Handling Errors

When success is false:

1. Read the error message to understand the problem
2. Check errorContext for specific details (line numbers, missing files, etc.)
3. Apply one of the suggestions to fix the issue
4. Retry the command

## Command Reference by Task

### Creating Records

| Task | Command |
|------|---------|
| Create plugin | esp create name [--light] [--author name] |
| Add quest | esp add-quest plugin editorId [--name name] [--start-enabled] |
| Add spell | esp add-spell plugin editorId [--name name] [--type type] |
| Add global | esp add-global plugin editorId [--type type] [--value n] |
| Attach script | esp attach-script plugin --quest id --script name |

### Script Operations

| Task | Command |
|------|---------|
| Generate template | papyrus generate --name name --extends type --output dir |
| Compile | papyrus compile source --output dir --headers dir |
| Decompile | papyrus decompile pex --output dir |
| Validate | papyrus validate psc |

### Asset Operations

| Task | Command |
|------|---------|
| NIF info | nif info file |
| List textures | nif textures file |
| Scale mesh | nif scale file factor --output file |
| Archive info | archive info archive |
| Extract archive | archive extract archive --output dir |
| Create archive | archive create directory --output file |

### Configuration

| Task | Command |
|------|---------|
| Create MCM | mcm create modName displayName --output file |
| Add toggle | mcm add-toggle config id text |
| Add slider | mcm add-slider config id text --min n --max n |
| Validate MCM | mcm validate config |

### SKSE Development

| Task | Command |
|------|---------|
| List templates | skse templates |
| Create project | skse create name --template template |
| Project info | skse info project |
| Add function | skse add-function project --name func --return type |

## Best Practices

### Naming Conventions

- Use unique prefixes for EditorIDs (e.g., MQM_ for My Quest Mod)
- Scripts should match the EditorID pattern: MQM_MainQuestScript
- Globals use similar patterns: MQM_Enabled, MQM_DebugMode

### File Organization

```
MyMod/
  MyMod.esp              # Plugin file
  MyMod.seq              # SEQ file (if start-enabled quests)
  Scripts/
    Source/              # PSC source files
      MQM_*.psc
    MQM_*.pex            # Compiled scripts
  MCM/
    config/
      MyMod/
        config.json
```

### Error Prevention

1. Check tool status before compiling: papyrus status --json
2. Validate scripts before compiling: papyrus validate script --json
3. Validate MCM after modifications: mcm validate config --json
4. Get plugin info before modifications: esp info plugin --json

### Incremental Development

Build mods incrementally, testing each step:

1. Create plugin structure first
2. Add one record at a time
3. Generate and test scripts individually
4. Add MCM configuration last
5. Package into archive when complete

## Troubleshooting

### Tool not found errors

```bash
# Download required tools
spookys-automod papyrus download --json
```

### Invalid header path during compilation

Ensure the Skyrim Scripts/Source path is correct and contains the vanilla .psc files.

### Record not found when attaching scripts

The quest or record must exist in the plugin. Create it first with esp add-quest.

### SKSE project will not build

1. Ensure CMake 3.21+ is installed
2. Ensure MSVC (Visual Studio Build Tools) is installed
3. Run vcpkg integrate install if using vcpkg
