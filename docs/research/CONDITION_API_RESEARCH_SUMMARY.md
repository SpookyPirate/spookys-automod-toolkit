# Mutagen Condition API Research Summary

**Date:** 2026-01-29
**Mutagen Version:** 0.45.1
**Purpose:** Determine exact API patterns for implementing condition support in Spooky's AutoMod Toolkit

---

## Executive Summary

The Mutagen Condition API is **fully functional** and ready for implementation. All CRUD operations (Create, Read, Update, Delete) are supported with straightforward patterns.

### Key Findings

1. **Condition Types**: Two concrete types exist: `ConditionFloat` and `ConditionGlobal`
2. **Record Support**: Not all record types have conditions - only Perk, Package, IdleAnimation, and MagicEffect
3. **Collection Type**: `Noggog.ExtendedList<Condition>` with full manipulation support
4. **Function Types**: 424 concrete `ConditionData` subclasses corresponding to Skyrim condition functions
5. **End-to-End**: Successfully created, saved, and reloaded a plugin with conditions

---

## 1. Record Types That Support Conditions

| Record Type | Has Conditions | Property Type |
|-------------|----------------|---------------|
| Perk | ✓ | `ExtendedList<Condition>` |
| Package | ✓ | `ExtendedList<Condition>` |
| IdleAnimation | ✓ | `ExtendedList<Condition>` |
| MagicEffect | ✓ | `ExtendedList<Condition>` |
| Spell | ✗ | N/A |
| Weapon | ✗ | N/A |
| Armor | ✗ | N/A |
| Activator | ✗ | N/A |

**Note:** Effects within Spells may have conditions, but Spell records themselves do not.

---

## 2. Condition Class Hierarchy

```
Condition (abstract base class)
├── ConditionFloat (concrete)
│   └── ComparisonValue: float
└── ConditionGlobal (concrete)
    └── ComparisonValue: IFormLink<IGlobalGetter>
```

### Common Properties (on base Condition class)

```csharp
public abstract class Condition
{
    public ConditionData Data { get; set; }        // The function (GetLevel, HasPerk, etc.)
    public CompareOperator CompareOperator { get; set; }  // ==, !=, >, <, >=, <=
    public Flag Flags { get; set; }                 // Various flags
    public MemorySlice<byte> Unknown1 { get; set; } // Binary data
    public ushort Unknown2 { get; set; }            // Unknown field
}
```

---

## 3. Creating Conditions

### Pattern: ConditionFloat (Most Common)

```csharp
var condition = new ConditionFloat
{
    ComparisonValue = 10.0f,
    CompareOperator = CompareOperator.GreaterThanOrEqualTo,
    Data = new GetLevelConditionData()
};

perk.Conditions.Add(condition);
```

### Pattern: ConditionGlobal (For Global Variables)

```csharp
var condition = new ConditionGlobal
{
    ComparisonValue = someGlobal.ToLink<IGlobalGetter>(),
    CompareOperator = CompareOperator.EqualTo,
    Data = new GetGlobalValueConditionData()
};
```

---

## 4. ConditionData Types (Functions)

### Examples of Common Functions

| Skyrim Function | C# Type | Parameters |
|-----------------|---------|------------|
| GetLevel | `GetLevelConditionData` | (none) |
| GetActorValue | `GetActorValueConditionData` | `ActorValue` (enum) |
| HasPerk | `HasPerkConditionData` | `Perk` (FormLinkOrIndex) |
| HasKeyword | `HasKeywordConditionData` | `Keyword` (FormLinkOrIndex) |
| IsSneaking | `IsSneakingConditionData` | (none) |
| GetGlobalValue | `GetGlobalValueConditionData` | (none - uses ComparisonValue) |

### Common ConditionData Properties

Most ConditionData types inherit these properties:

```csharp
public RunOnType RunOnType { get; set; }
public IFormLink<ISkyrimMajorRecordGetter>? Reference { get; set; }
public int Unknown3 { get; set; }
public bool UseAliases { get; set; }
public bool UsePackageData { get; set; }
```

### Total Count

- **424 concrete ConditionData types** available
- Naming pattern: `{FunctionName}ConditionData`
- All have parameterless constructors

---

## 5. Collection Operations

### Reading Conditions

```csharp
// Get count
int count = perk.Conditions.Count;

// Iterate
foreach (var condition in perk.Conditions)
{
    if (condition is ConditionFloat condFloat)
    {
        Console.WriteLine($"Value: {condFloat.ComparisonValue}");
        Console.WriteLine($"Operator: {condition.CompareOperator}");
        Console.WriteLine($"Function: {condition.Data?.GetType().Name}");
    }
}

// Index access
var firstCondition = perk.Conditions[0];
```

### Adding Conditions

```csharp
// Add new
perk.Conditions.Add(new ConditionFloat { ... });

// Insert at position
perk.Conditions.Insert(0, new ConditionFloat { ... });
```

### Removing Conditions

```csharp
// Remove by index
perk.Conditions.RemoveAt(1);

// Remove by predicate
int removed = perk.Conditions.RemoveAll(c =>
    c.CompareOperator == CompareOperator.EqualTo);

// Clear all
perk.Conditions.Clear();

// Remove specific instance
perk.Conditions.Remove(someCondition);
```

### Modifying Conditions

```csharp
// Direct modification (reference type)
var condition = perk.Conditions[0] as ConditionFloat;
if (condition != null)
{
    condition.ComparisonValue = 20.0f;
    condition.CompareOperator = CompareOperator.GreaterThan;
    // Changes are automatically reflected in the collection
}
```

---

## 6. Working with ConditionData Parameters

### Example: GetActorValue

```csharp
var data = new GetActorValueConditionData
{
    ActorValue = ActorValue.Health,
    RunOnType = RunOnType.Subject
};

var condition = new ConditionFloat
{
    ComparisonValue = 50.0f,
    CompareOperator = CompareOperator.GreaterThan,
    Data = data
};
```

### Example: HasKeyword (with FormLinkOrIndex)

**IMPORTANT:** `FormLink<T>` cannot implicitly convert to `IFormLinkOrIndex<T>`. This is a known limitation in Mutagen.

**Workaround Options:**

1. Use simpler functions that don't require FormLinkOrIndex
2. Create wrapper that handles the conversion
3. Set the property using reflection (not recommended)

**Functions that work directly:**

- `GetLevelConditionData`
- `GetActorValueConditionData` (uses ActorValue enum)
- `IsSneakingConditionData`
- `IsSwimmingConditionData`
- `IsRunningConditionData`
- And 400+ others

---

## 7. CompareOperator Enum

```csharp
public enum CompareOperator
{
    EqualTo = 0,
    NotEqualTo = 1,
    GreaterThan = 2,
    GreaterThanOrEqualTo = 3,
    LessThan = 4,
    LessThanOrEqualTo = 5
}
```

---

## 8. Complete Working Example

```csharp
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

// Create mod
var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);

// Create perk
var perk = mod.Perks.AddNew();
perk.EditorID = "TestPerk";

// Add condition: Level >= 10
perk.Conditions.Add(new ConditionFloat
{
    ComparisonValue = 10.0f,
    CompareOperator = CompareOperator.GreaterThanOrEqualTo,
    Data = new GetLevelConditionData()
});

// Add condition: Health > 50
perk.Conditions.Add(new ConditionFloat
{
    ComparisonValue = 50.0f,
    CompareOperator = CompareOperator.GreaterThan,
    Data = new GetActorValueConditionData
    {
        ActorValue = ActorValue.Health
    }
});

// Save
mod.WriteToBinary("Test.esp");

// Reload and verify
using var reloaded = SkyrimMod.CreateFromBinaryOverlay("Test.esp", SkyrimRelease.SkyrimSE);
var reloadedPerk = reloaded.Perks.First();
Console.WriteLine($"Conditions: {reloadedPerk.Conditions.Count}"); // Output: 2
```

**Test Result:** ✓ Successfully created, saved, and reloaded plugin with conditions

---

## 9. Implementation Recommendations for AutoMod Toolkit

### Commands to Implement

1. **`esp list-conditions <plugin> <record-type> <editor-id>`**
   - Read and display all conditions on a record
   - JSON output with function name, operator, value, parameters

2. **`esp add-condition <plugin> <record-type> <editor-id> --function <name> --operator <op> --value <val>`**
   - Create and add condition to record
   - Support common functions first (GetLevel, GetActorValue, IsSneaking)

3. **`esp remove-condition <plugin> <record-type> <editor-id> --index <n>`**
   - Remove condition by index
   - Optional: `--all` flag to clear all

4. **`esp modify-condition <plugin> <record-type> <editor-id> --index <n> --value <val>`**
   - Modify condition operator/value
   - Keep existing function/data

### Service Layer Pattern

```csharp
// ConditionService.cs
public class ConditionService
{
    public Result<List<ConditionInfo>> ListConditions(string pluginPath, string recordType, string editorId)
    {
        // Load plugin, find record, return condition list
    }

    public Result<string> AddCondition(string pluginPath, string recordType, string editorId,
        string function, string operatorStr, float value)
    {
        // Create condition, add to record, save
    }

    public Result<string> RemoveCondition(string pluginPath, string recordType, string editorId, int index)
    {
        // Load, remove by index, save
    }

    public Result<string> ModifyCondition(string pluginPath, string recordType, string editorId,
        int index, string? operatorStr, float? value)
    {
        // Load, modify in place, save
    }
}

public class ConditionInfo
{
    public string Function { get; set; }
    public string Operator { get; set; }
    public float Value { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}
```

### Function Name Mapping

**Pattern:** Strip "ConditionData" suffix from type name

```csharp
var functionName = conditionData.GetType().Name.Replace("ConditionData", "");
// "GetLevelConditionData" → "GetLevel"

// Reverse mapping
var typeName = $"{functionName}ConditionData";
var type = Assembly.GetType($"Mutagen.Bethesda.Skyrim.{typeName}");
```

### Limitation: FormLinkOrIndex

Some condition functions use `IFormLinkOrIndex<T>` parameters (HasPerk, HasKeyword, etc.). Direct assignment from `FormLink<T>` **does not work**.

**Options:**

1. **Phase 1**: Support only functions without FormLinkOrIndex (80%+ of use cases)
2. **Phase 2**: Implement conversion/wrapper for FormLinkOrIndex types
3. **Documentation**: Clearly state which functions are supported

---

## 10. Testing Evidence

All patterns verified with:

- ✓ Type inspection via reflection
- ✓ Instance creation and manipulation
- ✓ Adding conditions to records
- ✓ Removing conditions (by index, by predicate, clear all)
- ✓ Modifying conditions in place
- ✓ Saving to binary .esp file
- ✓ Reloading and verifying persistence

**Test File:** `ConditionApiTest/ConditionApiTest/` (standalone project)
**Output:** `ConditionApiTestResults.txt` (full test output)

---

## 11. Conclusion

**VERDICT: Ready for implementation**

The Mutagen Condition API provides complete CRUD support for conditions. All necessary operations are available:

- ✓ Reading conditions from records
- ✓ Creating new conditions
- ✓ Adding conditions to records
- ✓ Removing conditions
- ✓ Modifying existing conditions
- ✓ Persisting changes to disk

**Recommended Implementation Order:**

1. `list-conditions` - Read-only, easiest to implement
2. `remove-condition` - Simple collection manipulation
3. `add-condition` - More complex, start with simple functions (GetLevel, IsSneaking)
4. `modify-condition` - Builds on previous commands

**Estimated Implementation Time:**

- Core service layer: 2-4 hours
- CLI commands: 2-3 hours
- Testing & documentation: 2-3 hours
- **Total: 6-10 hours**

---

## Appendix A: Record Type Detection

To check if a record type supports conditions at runtime:

```csharp
public static bool SupportsConditions(ISkyrimMajorRecord record)
{
    var type = record.GetType();
    var prop = type.GetProperty("Conditions");
    return prop != null;
}
```

Supported types (verified):

- `Perk`
- `Package`
- `IdleAnimation`
- `MagicEffect`

---

## Appendix B: All Available ConditionData Types

**Total: 424 types**

Sample (first 50):

- CanFlyHereConditionData
- CanHaveFlamesConditionData
- CanPayCrimeGoldConditionData
- DoesNotExistConditionData
- EffectWasDualCastConditionData
- EPAlchemyEffectHasKeywordConditionData
- EPAlchemyGetMakingPoisonConditionData
- EPMagic_IsAdvanceSkillConditionData
- EPMagic_SpellHasKeywordConditionData
- EPMagic_SpellHasSkillConditionData
- GetActorAggressionConditionData
- GetActorValueConditionData
- GetActorValuePercentConditionData
- GetAngleConditionData
- GetBaseActorValueConditionData
- GetClassSkillLevelConditionData
- GetCombatStateConditionData
- GetCurrentTimeConditionData
- GetDayOfWeekConditionData
- GetDeadCountConditionData
- GetDestructionStageConditionData
- GetDispositionConditionData
- GetDistanceConditionData
- GetEquippedConditionData
- GetFactionRankConditionData
- GetGlobalValueConditionData
- GetGoldAmountConditionData
- GetHeadingAngleConditionData
- GetHealthPercentageConditionData
- GetIdleDoneOnceConditionData
- GetInCurrentLocConditionData
- GetInFactionConditionData
- GetInSameGroupAsConditionData
- GetIsAliasRefConditionData
- GetIsClassConditionData
- GetIsCreatureConditionData
- GetIsCurrentSpellConditionData
- GetIsIDConditionData
- GetIsPlayableRaceConditionData
- GetIsRaceConditionData
- GetIsReferenceConditionData
- GetIsSexConditionData
- GetIsUsedItemConditionData
- GetIsVoiceTypeConditionData
- GetItemCountConditionData
- GetKnockedStateConditionData
- GetLevelConditionData
- GetLineOfSightConditionData
- GetLockedConditionData
- GetMajorCrimeValueConditionData

(See `ConditionApiTestResults.txt` for complete list)

---

**END OF RESEARCH SUMMARY**
