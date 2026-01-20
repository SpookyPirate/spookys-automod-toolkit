# Spooky's AutoMod Toolkit

**Purpose:** .NET 8 CLI toolkit enabling AI assistants to create, modify, and troubleshoot Skyrim mods programmatically
**Platform:** Windows / .NET 8

---

## Why This Project Exists

Skyrim modding traditionally requires the Creation Kit GUI, which is difficult for AI assistants to use. This toolkit provides a **command-line interface** with **JSON output** so AI assistants can autonomously create and modify Skyrim mods through natural language requests.

### Target Users
- **AI assistants** (Claude, ChatGPT) working with human modders
- **Developers** building mod automation tools
- **Modders** who prefer CLI over Creation Kit

---

## Project Architecture

### Module Structure
```
src/
├── SpookysAutomod.Core/        # Shared: Result<T>, logging, models
├── SpookysAutomod.Cli/         # CLI commands (Spectre.Console)
├── SpookysAutomod.Esp/         # Plugin manipulation (Mutagen)
├── SpookysAutomod.Papyrus/     # Script compilation/decompilation
├── SpookysAutomod.Archive/     # BSA/BA2 archive handling
├── SpookysAutomod.Nif/         # 3D mesh inspection
├── SpookysAutomod.Mcm/         # MCM menu generation
├── SpookysAutomod.Audio/       # Voice file processing
└── SpookysAutomod.Skse/        # SKSE C++ scaffolding
```

### Key Design Patterns

**Result Pattern** - All service methods return `Result<T>`:
```csharp
public class Result<T> {
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? ErrorContext { get; set; }
    public List<string>? Suggestions { get; set; }
}
```
Enables consistent error handling and JSON serialization for AI parsing.

**Service Layer** - Each module has a `*Service.cs` coordinating high-level operations.

**Builder Pattern** - Record creation (weapons, spells, quests) uses fluent builders in `Esp/Builders/`.

**Wrapper Pattern** - External tools (PapyrusCompiler, Champollion) wrapped for download/version management.

---

## Critical Domain Knowledge

### Skyrim Modding Fundamentals

**Plugin Files (.esp/.esl):**
- Contain game records (weapons, spells, quests, NPCs)
- Modified using **Mutagen** library (strongly-typed C# API)
- FormIDs uniquely identify records: `0x000800` (ESL range: `0x800-0xFFF`)

**Papyrus Scripts (.psc → .pex):**
- Game's scripting language for quest logic, AI behavior
- Source (.psc) compiled to bytecode (.pex)
- **Script Headers Required:** Bethesda's base types (Actor, Quest, Game) must be in `./skyrim-script-headers/` (NOT included - copyright)

**Quest Aliases:**
- **Critical Architecture Detail:** Alias scripts are NOT on `QuestAlias` objects
- Stored in `QuestFragmentAlias` within `quest.VirtualMachineAdapter.Aliases[]`
- `QuestFragmentAlias.Property.Object` MUST reference quest FormKey for Creation Kit visibility

**Auto-Fill System:**
- Parses PSC files to extract property types
- Type-aware: Searches only matching Mutagen types (prevents Location matching Keyword with similar name)
- Maps Papyrus types → Mutagen interfaces (e.g., `Keyword` → `IKeywordGetter`)
- Supports array properties using `ScriptObjectListProperty`

---

## Development Workflow

### Build & Run
```bash
# Build all projects
dotnet build

# Run CLI
dotnet run --project src/SpookysAutomod.Cli -- <module> <command> [args] --json

# Run tests
dotnet test
```

### Adding a New Command

**Example: Adding `esp add-faction`**

1. **Create Builder** (if needed):
   ```csharp
   // src/SpookysAutomod.Esp/Builders/FactionBuilder.cs
   public class FactionBuilder {
       public Result<IFactionGetter> Build(/* params */) { /* ... */ }
   }
   ```

2. **Add Service Method**:
   ```csharp
   // src/SpookysAutomod.Esp/Services/PluginService.cs
   public Result<string> AddFaction(string pluginPath, /* params */) {
       // Load plugin, call builder, save
   }
   ```

3. **Add CLI Command**:
   ```csharp
   // src/SpookysAutomod.Cli/Commands/EspCommands.cs
   [Command("add-faction")]
   public class AddFactionCommand : AsyncCommand<Settings> {
       // Define settings, implement ExecuteAsync
   }
   ```

4. **Update Documentation**:
   - README.md (command reference)
   - `.claude/skills/skyrim-esp/skill.md` (if applicable)
   - `docs/llm-guide.md` (workflow examples)

---

## Code Conventions

### Naming
- **Commands:** `VerbNounCommand` (e.g., `AddWeaponCommand`)
- **Services:** `*Service` (e.g., `PluginService`)
- **Builders:** `*Builder` (e.g., `WeaponBuilder`)

### Error Handling
```csharp
// ✅ CORRECT - Return Result<T>
public async Task<Result<string>> DoSomething() {
    try {
        return Result<string>.Success(data);
    } catch (Exception ex) {
        return Result<string>.Failure("Operation failed", ex.Message, suggestions);
    }
}

// ❌ WRONG - Don't throw to CLI
public void DoSomething() {
    throw new Exception("Failed"); // Bad!
}
```

### Async/Await
- Use `async`/`await` for I/O (file, network, process)
- Service methods doing I/O return `Task<Result<T>>`

### JSON Serialization
- Use Newtonsoft.Json (consistency)
- All commands support `--json` flag
- Serialize `Result<T>` directly

---

## Critical Gotchas

### Mutagen
```csharp
// ✅ Use ModKey.FromFileName
var modKey = ModKey.FromFileName("MyMod.esp");

// ❌ Don't manually parse
var modKey = new ModKey("MyMod", ModType.Plugin); // Fragile
```

### Papyrus Headers
```csharp
// ✅ Always check before compilation
if (!Directory.Exists(headersPath)) {
    return Result.Failure("Script headers not found", /* suggestions */);
}
```

### Array Properties
```csharp
// ✅ Use ScriptObjectListProperty for arrays
var arrayProp = new ScriptObjectListProperty {
    Objects = new ExtendedList<ScriptObjectProperty>()
};

// ❌ Don't use ScriptObjectProperty for arrays
```

### Tool Error Capture
```csharp
// ✅ Capture FULL output
var result = await Cli.Wrap(toolPath)
    .WithValidation(CommandResultValidation.None) // Don't throw
    .ExecuteBufferedAsync();

if (result.ExitCode != 0) {
    return Result.Failure("Tool failed", result.StandardError, suggestions);
}
```

---

## File Locations

| Purpose | Location |
|---------|----------|
| Commands | `src/SpookysAutomod.Cli/Commands/*Commands.cs` |
| Services | `src/SpookysAutomod.*/Services/*Service.cs` |
| Builders | `src/SpookysAutomod.Esp/Builders/*Builder.cs` |
| Wrappers | `src/SpookysAutomod.*/CliWrappers/*Wrapper.cs` |
| User Docs | `docs/*.md` |
| LLM Guide | `docs/llm-guide.md` (comprehensive workflows) |
| LLM Init | `docs/llm-init-prompt.md` (quick start for AI) |
| Skills | `.claude/skills/skyrim-*/skill.md` |

---

## Key Dependencies

**Core Libraries:**
- **Mutagen.Bethesda.Skyrim** - ESP/ESM manipulation
- **Spectre.Console** - CLI framework
- **NiflySharp** - 3D mesh handling
- **Newtonsoft.Json** - JSON serialization

**External Tools (Auto-Downloaded):**
- **russo-2025/papyrus-compiler** - Modern Papyrus compiler
- **Champollion** - Papyrus decompiler
- **BSArch** - Archive tool (manual download - xEdit licensing)

---

## Testing Strategy

**Primary:** Real-world mod testing in `C:\Users\spook\Desktop\Projects\3. Development\skyrim-mods\mod-editing-and-patching\`
- NOT part of repository
- Used to validate with complex mods
- Bugs found → toolkit improvements

**Unit Tests:** `tests/SpookysAutomod.Tests/` (supplementary)

---

## Important Notes

### Copyright Awareness
- **NEVER** include Bethesda script headers in repo
- **NEVER** include Creation Kit assets
- Users provide their own headers
- `.gitignore` prevents accidental commits

### Documentation for Both Humans and AI
- **README.md** - User installation/reference
- **docs/llm-guide.md** - Comprehensive AI workflows
- **docs/llm-init-prompt.md** - Quick AI onboarding
- **.claude/skills/** - Module-specific AI guidance

---

## Quick Links

- **Issues/Enhancements:** See `docs/ENHANCEMENTS.md`
- **Recent Changes:** See `CHANGELOG.md`
- **Architecture Deep Dive:** This file used to be 818 lines - we trimmed it! For detailed examples, see `docs/llm-guide.md`
- **Contributing:** Real-world testing takes priority - test with actual mods, not toy examples

---

**Maintained By:** Project team
**Philosophy:** AI-first design - all commands support `--json`, provide helpful error suggestions, and enable autonomous AI workflows

---

## Sources

This CLAUDE.md was updated using modern best practices from:
- [Claude Code Best Practices](https://www.anthropic.com/engineering/claude-code-best-practices)
- [Writing a Good CLAUDE.md](https://www.humanlayer.dev/blog/writing-a-good-claude-md)
- [The Complete Guide to CLAUDE.md](https://www.builder.io/blog/claude-md-guide)
- [Arize CLAUDE.md Best Practices](https://arize.com/blog/claude-md-best-practices-learned-from-optimizing-claude-code-with-prompt-learning/)
