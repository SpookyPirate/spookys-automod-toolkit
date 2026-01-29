# Mutagen Conditions API Reference

**Last Updated:** 2026-01-29
**Mutagen Version:** 0.52.0
**Purpose:** Document how to work with Conditions in Mutagen.Bethesda.Skyrim

---

## Overview

Conditions in Skyrim control when records (spells, perks, weapons, armor) take effect. In Mutagen, conditions are accessed through strongly-typed interfaces and classes.

## Key Findings from Research

### 1. Which Records Have Conditions?

Based on codebase exploration and the commented code in `PluginService.cs` (lines 776-790):

**Records WITH Conditions Property:**
- `Perk` - `perk.Conditions`
- Additional records to verify (likely have Conditions):
  - `Spell`
  - `Weapon`
  - `Armor`
  - `MagicEffect`
  - `Ingestible`

**Records WITHOUT Direct Conditions:**
- Most records don't have a `Conditions` property directly

### 2. Condition Type Hierarchy

From `debug-types` output, Mutagen has numerous condition-related types:

**Condition Data Types (Sample):**
```
GetInCurrentLocAliasConditionData
GetIsAliasRefConditionData
GetIsEditorLocAliasConditionData
GetKeywordDataForAliasConditionData
GetLocAliasRefTypeAliveCountConditionData
... (157 total condition-related types found)
```

**Key Pattern:** Each game condition function (GetHasPerk, GetItemCount, GetIsRace, etc.) has its own `*ConditionData` class.

### 3. Condition Property Structure

From the commented code in `PluginService.cs` (lines 797-806), we can infer the structure:

```csharp
// Access conditions on a record
IReadOnlyList<IConditionGetter>? conditionList = spell.Conditions;

// Each condition has:
foreach (var condition in conditionList)
{
    var data = condition.Data;

    // Properties on Data:
    // - Function (enum of condition functions)
    // - ComparisonValue (float)
    // - CompareOperator (enum: ==, !=, >, <, >=, <=)
    // - Flags (condition flags)
    // - RunOnType (Subject, Target, Reference, etc.)

    // Parameters vary by condition function type
    // - ParameterOneRecord (FormLink to a record)
    // - ParameterTwo, etc.
}
```

### 4. Condition Data Properties

From `debug-types` output showing `GetIsAliasRefConditionData`:

```csharp
Properties:
  FirstUnusedStringParameter: String?
  Reference: IFormLink<ISkyrimMajorRecordGetter>?
  ReferenceAliasIndex: Int32
  RunOnType: RunOnType
  RunOnTypeIndex: Int32
  SecondUnusedIntParameter: Int32
  SecondUnusedStringParameter: String?
  UseAliases: Boolean
  UsePackageData: Boolean
```

**Common Properties Across Condition Data Types:**
- `Reference`: FormLink to a record
- `RunOnType`: Enum indicating what the condition runs on
- `RunOnTypeIndex`: Integer index for RunOnType
- `UseAliases`: Boolean flag
- `UsePackageData`: Boolean flag

---

## API Usage Patterns

### Reading Conditions

```csharp
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;

// Load a plugin
var mod = SkyrimMod.CreateFromBinaryOverlay(
    ModPath.FromPath("MyMod.esp"),
    SkyrimRelease.SkyrimSE);

// Access conditions on a perk
foreach (var perk in mod.Perks)
{
    if (perk.Conditions == null || perk.Conditions.Count == 0)
        continue;

    Console.WriteLine($"Perk: {perk.EditorID}");

    foreach (var condition in perk.Conditions)
    {
        var data = condition.Data;

        // Access common properties
        // Note: Exact property names need verification
        // Based on commented code and ConditionData types found

        Console.WriteLine($"  Function: {data.Function}");
        Console.WriteLine($"  ComparisonValue: {data.ComparisonValue}");
        Console.WriteLine($"  CompareOperator: {data.CompareOperator}");
        Console.WriteLine($"  Flags: {data.Flags}");
        Console.WriteLine($"  RunOnType: {data.RunOnType}");

        // Access parameters (vary by condition type)
        // if (data.ParameterOneRecord != null)
        // {
        //     Console.WriteLine($"  Parameter 1: {data.ParameterOneRecord}");
        // }
    }
}
```

### Creating Conditions (Unverified Pattern)

```csharp
// Create a new perk
var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
var perk = mod.Perks.AddNew("TestPerk");

// Pattern needs verification - likely something like:
// var condition = new Condition();
// condition.Data = new GetHasPerkConditionData
// {
//     Perk = new FormLink<IPerkGetter>(perkFormKey),
//     ComparisonValue = 1.0f,
//     CompareOperator = CompareOperator.EqualTo,
//     RunOnType = RunOnType.Subject
// };
//
// perk.Conditions.Add(condition);

// Save the mod
mod.WriteToBinary("Test.esp");
```

### Modifying Conditions

```csharp
// Load for modification
var mod = SkyrimMod.CreateFromBinary(
    ModPath.FromPath("MyMod.esp"),
    SkyrimRelease.SkyrimSE);

var perk = mod.Perks.First(p => p.EditorID == "TestPerk");

// Modify existing condition
if (perk.Conditions != null && perk.Conditions.Count > 0)
{
    var condition = perk.Conditions[0];
    // Modify properties
    // condition.Data.ComparisonValue = 2.0f;
}

mod.WriteToBinary("MyMod.esp");
```

### Removing Conditions

```csharp
// Clear all conditions
perk.Conditions.Clear();

// Remove specific condition
perk.Conditions.RemoveAt(0);

// Remove by predicate
// perk.Conditions.RemoveWhere(c => /* condition */);
```

---

## Condition Function Types

From research, each game condition function has its own data type:

**Common Condition Functions:**
- `GetHasPerk` - Check if actor has a perk
- `GetItemCount` - Check item count in inventory
- `GetIsRace` - Check actor's race
- `GetLevel` - Check actor level
- `GetIsAliasRef` - Check if reference matches alias
- `GetInCurrentLoc` - Check current location
- `GetInCurrentLocAlias` - Check location alias
- `GetKeywordDataForAlias` - Check keyword on alias
- ... (many more - 157 types found)

**Pattern:** `Get<FunctionName>ConditionData` class for each function

---

## CompareOperator Enum (Inferred)

```csharp
enum CompareOperator
{
    EqualTo,          // == 0
    NotEqualTo,       // != 1
    GreaterThan,      // >  2
    GreaterThanOrEqual, // >= 3
    LessThan,         // <  4
    LessThanOrEqual   // <= 5
}
```

---

## RunOnType Enum (Inferred)

```csharp
enum RunOnType
{
    Subject,      // The subject of the condition
    Target,       // The target
    Reference,    // A specific reference
    CombatTarget, // Combat target
    LinkedReference, // Linked ref
    QuestAlias,   // Quest alias
    PackageData,  // Package data
    EventData     // Event data
}
```

---

## Flags (Inferred from Common Patterns)

Condition flags control evaluation behavior:
- `OR` - Condition is OR'd with previous (default AND)
- `UseGlobal` - Use global variable for comparison
- `SwapSubjectAndTarget` - Swap subject/target
- `RunOnTarget` - Run on target instead of subject

---

## Code Examples from Codebase

### Example 1: Reading Conditions (PluginService.cs lines 797-809)

```csharp
foreach (var condition in conditionList)
{
    var condInfo = new ConditionInfo
    {
        FunctionName = condition.Data.Function.ToString(),
        ComparisonValue = condition.Data.ComparisonValue,
        Operator = ((int)condition.Data.CompareOperator).ToString(),
        Flags = condition.Data.Flags.ToString(),
        RunOn = condition.Data.RunOnType.ToString()
    };

    if (condition.Data.ParameterOneRecord != null)
    {
        // Handle parameter records
    }
}
```

---

## Research Notes

### TODO: Verify These APIs

1. **Exact property names** on `IConditionGetter` and `IConditionDataGetter`
2. **Constructor patterns** for creating new conditions
3. **Specific ConditionData types** for common functions (GetHasPerk, GetItemCount, etc.)
4. **Parameter naming** (ParameterOne vs FirstParameter, etc.)
5. **How to specify condition function** when creating new conditions

### Research Methods Used

1. **Codebase Search:**
   - Found commented condition code in `PluginService.cs:767-809`
   - Found `PerkBuilder.cs` with no condition examples

2. **Type Inspection:**
   - `debug-types "*Condition*"` found 157 condition-related types
   - All follow pattern: `Get<Function>ConditionData`

3. **Web Research:**
   - [Mutagen GitHub](https://github.com/Mutagen-Modding/Mutagen)
   - [Mutagen Documentation](https://mutagen-modding.github.io/Mutagen/)
   - [Big Cheat Sheet](https://github.com/Mutagen-Modding/Mutagen/wiki/Big-Cheat-Sheet)
   - No specific condition examples found in public documentation

4. **Recommended Next Steps:**
   - Join Mutagen Discord for API clarification
   - Examine existing Synthesis patchers that modify conditions
   - Use reflection to inspect actual types at runtime
   - Test with Creation Kit to verify behavior

---

## References

**Mutagen Resources:**
- [Mutagen GitHub Repository](https://github.com/Mutagen-Modding/Mutagen)
- [Mutagen Documentation](https://mutagen-modding.github.io/Mutagen/)
- [Synthesis Framework](https://github.com/Mutagen-Modding/Synthesis)

**Skyrim Modding:**
- Conditions are stored as CTDA subrecords in ESP format
- Each condition has a function index, comparison operator, and parameters
- Mutagen abstracts this into strongly-typed C# classes

**This Project:**
- `src/SpookysAutomod.Esp/Services/PluginService.cs:767-809` - Commented condition extraction code
- `src/SpookysAutomod.Esp/Research/ConditionApiExplorer.cs` - Type inspection tools
- `src/SpookysAutomod.Cli/Commands/EspCommands.cs:1315+` - `debug-types` command

---

## Conclusion

**What We Know:**
- Conditions exist on Perk (confirmed) and likely Spell, Weapon, Armor, MagicEffect
- 157 condition-related types exist in Mutagen.Bethesda.Skyrim
- Each game function has a dedicated `*ConditionData` class
- Conditions have Function, ComparisonValue, CompareOperator, Flags, RunOnType, and parameters

**What Needs Verification:**
- Exact API for creating new conditions
- Specific ConditionData constructors and required properties
- How to set condition function (enum? property?)
- Parameter naming conventions
- Whether Spell/Weapon/Armor actually have Conditions property

**Recommended Approach:**
1. Use reflection tools in `ConditionApiExplorer.cs` to inspect at runtime
2. Load a vanilla ESP with known conditions and inspect structure
3. Consult Mutagen Discord/community for guidance
4. Test condition creation and verify in Creation Kit

---

*This document will be updated as API details are verified through testing and implementation.*
