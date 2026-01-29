# Condition API Quick Reference

**For rapid implementation - see CONDITION_API_RESEARCH_SUMMARY.md for full details**

---

## Record Types With Conditions

```csharp
// Only these 4 types support conditions:
Perk
Package
IdleAnimation
MagicEffect
```

---

## Creating a Condition

```csharp
// Pattern 1: Simple condition (no parameters)
var condition = new ConditionFloat
{
    ComparisonValue = 10.0f,
    CompareOperator = CompareOperator.GreaterThanOrEqualTo,
    Data = new GetLevelConditionData()
};
perk.Conditions.Add(condition);

// Pattern 2: Condition with parameters
var condition = new ConditionFloat
{
    ComparisonValue = 50.0f,
    CompareOperator = CompareOperator.GreaterThan,
    Data = new GetActorValueConditionData
    {
        ActorValue = ActorValue.Health
    }
};
perk.Conditions.Add(condition);
```

---

## Reading Conditions

```csharp
// Count
int count = perk.Conditions.Count;

// Iterate
foreach (var cond in perk.Conditions)
{
    if (cond is ConditionFloat condFloat)
    {
        var value = condFloat.ComparisonValue;
        var op = cond.CompareOperator;
        var function = cond.Data?.GetType().Name.Replace("ConditionData", "");
    }
}

// Index access
var first = perk.Conditions[0];
```

---

## Removing Conditions

```csharp
// By index
perk.Conditions.RemoveAt(1);

// By predicate
perk.Conditions.RemoveAll(c => c.CompareOperator == CompareOperator.EqualTo);

// Clear all
perk.Conditions.Clear();
```

---

## Modifying Conditions

```csharp
// Get reference and modify
var condition = perk.Conditions[0] as ConditionFloat;
if (condition != null)
{
    condition.ComparisonValue = 20.0f;
    condition.CompareOperator = CompareOperator.LessThan;
    // Changes automatically reflected in collection
}
```

---

## CompareOperator Values

```csharp
CompareOperator.EqualTo                  // ==
CompareOperator.NotEqualTo               // !=
CompareOperator.GreaterThan              // >
CompareOperator.GreaterThanOrEqualTo     // >=
CompareOperator.LessThan                 // <
CompareOperator.LessThanOrEqualTo        // <=
```

---

## Common ConditionData Types (No FormLink Issues)

```csharp
// Level checks
new GetLevelConditionData()

// Actor values
new GetActorValueConditionData { ActorValue = ActorValue.Health }
new GetActorValueConditionData { ActorValue = ActorValue.Magicka }
new GetActorValueConditionData { ActorValue = ActorValue.Stamina }

// States
new IsSneakingConditionData()
new IsSwimmingConditionData()
new IsRunningConditionData()
new IsSprintingConditionData()
new IsInCombatConditionData()

// Time/weather
new GetCurrentTimeConditionData()
new GetCurrentWeatherConditionData()

// Gold/items
new GetGoldAmountConditionData()
new GetItemCountConditionData { Item = formLink }
```

---

## Function Name → Type Mapping

```csharp
// To create from string
var functionName = "GetLevel";
var typeName = $"{functionName}ConditionData";
var type = typeof(ISkyrimMod).Assembly.GetType($"Mutagen.Bethesda.Skyrim.{typeName}");
var instance = Activator.CreateInstance(type) as ConditionData;

// To get function name from instance
var functionName = conditionData.GetType().Name.Replace("ConditionData", "");
```

---

## Check If Record Supports Conditions

```csharp
public static bool SupportsConditions(ISkyrimMajorRecord record)
{
    var prop = record.GetType().GetProperty("Conditions");
    return prop != null;
}
```

---

## Known Limitation

**FormLinkOrIndex** - Some functions (HasPerk, HasKeyword) use `IFormLinkOrIndex<T>` which cannot be directly assigned from `FormLink<T>`.

**Workaround:** Start with functions that don't use FormLinkOrIndex. This covers 80%+ of use cases.

---

## CLI Command Patterns

```bash
# List conditions
spookys-automod esp list-conditions MyMod.esp Perk MyPerkID --json

# Add condition
spookys-automod esp add-condition MyMod.esp Perk MyPerkID \
    --function GetLevel --operator ">=" --value 10

# Remove condition
spookys-automod esp remove-condition MyMod.esp Perk MyPerkID --index 0

# Modify condition
spookys-automod esp modify-condition MyMod.esp Perk MyPerkID \
    --index 0 --operator ">" --value 20
```

---

## Full Working Example

```csharp
var mod = new SkyrimMod(ModKey.FromFileName("Test.esp"), SkyrimRelease.SkyrimSE);
var perk = mod.Perks.AddNew();
perk.EditorID = "TestPerk";

// Add level requirement
perk.Conditions.Add(new ConditionFloat
{
    ComparisonValue = 10.0f,
    CompareOperator = CompareOperator.GreaterThanOrEqualTo,
    Data = new GetLevelConditionData()
});

// Save
mod.WriteToBinary("Test.esp");
```

---

**See CONDITION_API_RESEARCH_SUMMARY.md for:**

- Complete list of 424 ConditionData types
- Detailed parameter information
- Testing evidence
- Implementation recommendations
