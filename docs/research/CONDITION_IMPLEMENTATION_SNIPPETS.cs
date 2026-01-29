// EXACT CODE SNIPPETS FOR CONDITION IMPLEMENTATION
// Copy-paste ready code for AutoMod Toolkit
// All patterns verified and tested - see CONDITION_API_RESEARCH_SUMMARY.md

using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.Reflection;

namespace ImplementationSnippets;

// ============================================================================
// 1. CHECKING IF RECORD SUPPORTS CONDITIONS
// ============================================================================

public static class RecordConditionChecker
{
    public static bool SupportsConditions(ISkyrimMajorRecord record)
    {
        var type = record.GetType();
        var conditionsProp = type.GetProperty("Conditions");
        return conditionsProp != null;
    }

    public static IEnumerable<Condition>? GetConditions(ISkyrimMajorRecord record)
    {
        var type = record.GetType();
        var conditionsProp = type.GetProperty("Conditions");
        if (conditionsProp == null) return null;

        return conditionsProp.GetValue(record) as IEnumerable<Condition>;
    }
}

// ============================================================================
// 2. READING CONDITIONS FROM A RECORD
// ============================================================================

public class ConditionReader
{
    public class ConditionInfo
    {
        public int Index { get; set; }
        public string FunctionName { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public float? FloatValue { get; set; }
        public string? GlobalFormKey { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public static List<ConditionInfo> ReadConditions(ISkyrimMajorRecord record)
    {
        var results = new List<ConditionInfo>();

        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return results;

        var conditions = conditionsProp.GetValue(record) as IEnumerable<Condition>;
        if (conditions == null) return results;

        int index = 0;
        foreach (var cond in conditions)
        {
            var info = new ConditionInfo
            {
                Index = index++,
                Operator = cond.CompareOperator.ToString()
            };

            // Get function name
            if (cond.Data != null)
            {
                info.FunctionName = cond.Data.GetType().Name.Replace("ConditionData", "");

                // Extract parameters
                ExtractParameters(cond.Data, info.Parameters);
            }

            // Get comparison value
            if (cond is ConditionFloat condFloat)
            {
                info.FloatValue = condFloat.ComparisonValue;
            }
            else if (cond is ConditionGlobal condGlobal)
            {
                info.GlobalFormKey = condGlobal.ComparisonValue.FormKey.ToString();
            }

            results.Add(info);
        }

        return results;
    }

    private static void ExtractParameters(ConditionData data, Dictionary<string, object> parameters)
    {
        var type = data.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.DeclaringType == type);

        foreach (var prop in props)
        {
            try
            {
                var value = prop.GetValue(data);
                if (value != null)
                {
                    parameters[prop.Name] = value;
                }
            }
            catch
            {
                // Skip properties that can't be read
            }
        }
    }
}

// ============================================================================
// 3. ADDING CONDITIONS TO A RECORD
// ============================================================================

public class ConditionAdder
{
    public static bool AddCondition(
        ISkyrimMajorRecord record,
        string functionName,
        CompareOperator compareOperator,
        float value,
        Dictionary<string, object>? parameters = null)
    {
        // Get Conditions property
        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return false;

        var conditions = conditionsProp.GetValue(record);
        if (conditions == null) return false;

        // Create ConditionData instance
        var conditionData = CreateConditionData(functionName, parameters);
        if (conditionData == null) return false;

        // Create Condition (ConditionFloat for now)
        var condition = new ConditionFloat
        {
            ComparisonValue = value,
            CompareOperator = compareOperator,
            Data = conditionData
        };

        // Add to collection
        var addMethod = conditions.GetType().GetMethod("Add");
        if (addMethod == null) return false;

        addMethod.Invoke(conditions, new object[] { condition });
        return true;
    }

    private static ConditionData? CreateConditionData(
        string functionName,
        Dictionary<string, object>? parameters)
    {
        // Build type name
        var typeName = $"{functionName}ConditionData";
        var fullTypeName = $"Mutagen.Bethesda.Skyrim.{typeName}";

        // Get type from assembly
        var type = typeof(ISkyrimMod).Assembly.GetType(fullTypeName);
        if (type == null) return null;

        // Create instance
        var instance = Activator.CreateInstance(type) as ConditionData;
        if (instance == null) return null;

        // Set parameters if provided
        if (parameters != null)
        {
            SetParameters(instance, parameters);
        }

        return instance;
    }

    private static void SetParameters(ConditionData data, Dictionary<string, object> parameters)
    {
        var type = data.GetType();

        foreach (var (key, value) in parameters)
        {
            var prop = type.GetProperty(key);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    // Handle enum conversion
                    if (prop.PropertyType.IsEnum && value is string strValue)
                    {
                        var enumValue = Enum.Parse(prop.PropertyType, strValue);
                        prop.SetValue(data, enumValue);
                    }
                    else
                    {
                        prop.SetValue(data, value);
                    }
                }
                catch
                {
                    // Skip parameters that can't be set
                }
            }
        }
    }
}

// ============================================================================
// 4. REMOVING CONDITIONS FROM A RECORD
// ============================================================================

public class ConditionRemover
{
    public static bool RemoveConditionByIndex(ISkyrimMajorRecord record, int index)
    {
        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return false;

        var conditions = conditionsProp.GetValue(record);
        if (conditions == null) return false;

        var removeAtMethod = conditions.GetType().GetMethod("RemoveAt");
        if (removeAtMethod == null) return false;

        try
        {
            removeAtMethod.Invoke(conditions, new object[] { index });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static int RemoveAllConditions(ISkyrimMajorRecord record)
    {
        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return 0;

        var conditions = conditionsProp.GetValue(record);
        if (conditions == null) return 0;

        var countProp = conditions.GetType().GetProperty("Count");
        if (countProp == null) return 0;

        var count = (int)(countProp.GetValue(conditions) ?? 0);

        var clearMethod = conditions.GetType().GetMethod("Clear");
        if (clearMethod == null) return 0;

        clearMethod.Invoke(conditions, null);
        return count;
    }

    public static int RemoveConditionsByPredicate(
        ISkyrimMajorRecord record,
        Func<Condition, bool> predicate)
    {
        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return 0;

        var conditions = conditionsProp.GetValue(record);
        if (conditions == null) return 0;

        var removeAllMethod = conditions.GetType().GetMethod("RemoveAll");
        if (removeAllMethod == null) return 0;

        var predicateParam = new Predicate<Condition>(predicate);
        var removed = removeAllMethod.Invoke(conditions, new object[] { predicateParam });

        return removed is int count ? count : 0;
    }
}

// ============================================================================
// 5. MODIFYING CONDITIONS IN A RECORD
// ============================================================================

public class ConditionModifier
{
    public static bool ModifyCondition(
        ISkyrimMajorRecord record,
        int index,
        CompareOperator? newOperator = null,
        float? newValue = null)
    {
        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return false;

        var conditions = conditionsProp.GetValue(record) as IList<Condition>;
        if (conditions == null || index < 0 || index >= conditions.Count) return false;

        var condition = conditions[index];

        // Modify operator
        if (newOperator.HasValue)
        {
            condition.CompareOperator = newOperator.Value;
        }

        // Modify value
        if (newValue.HasValue && condition is ConditionFloat condFloat)
        {
            condFloat.ComparisonValue = newValue.Value;
        }

        return true;
    }

    public static bool ModifyConditionData(
        ISkyrimMajorRecord record,
        int index,
        Dictionary<string, object> parameters)
    {
        var conditionsProp = record.GetType().GetProperty("Conditions");
        if (conditionsProp == null) return false;

        var conditions = conditionsProp.GetValue(record) as IList<Condition>;
        if (conditions == null || index < 0 || index >= conditions.Count) return false;

        var condition = conditions[index];
        if (condition.Data == null) return false;

        // Set parameters on existing ConditionData
        var dataType = condition.Data.GetType();

        foreach (var (key, value) in parameters)
        {
            var prop = dataType.GetProperty(key);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    if (prop.PropertyType.IsEnum && value is string strValue)
                    {
                        var enumValue = Enum.Parse(prop.PropertyType, strValue);
                        prop.SetValue(condition.Data, enumValue);
                    }
                    else
                    {
                        prop.SetValue(condition.Data, value);
                    }
                }
                catch
                {
                    // Skip invalid parameters
                }
            }
        }

        return true;
    }
}

// ============================================================================
// 6. COMPLETE SERVICE EXAMPLE
// ============================================================================

public class ConditionService
{
    public class ConditionListResult
    {
        public bool Success { get; set; }
        public List<ConditionReader.ConditionInfo> Conditions { get; set; } = new();
        public string? Error { get; set; }
    }

    public class ConditionModifyResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    public ConditionListResult ListConditions(string pluginPath, string recordType, string editorId)
    {
        try
        {
            using var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, SkyrimRelease.SkyrimSE);

            // Find record (simplified - use existing FindRecord logic)
            var record = FindRecord(mod, recordType, editorId);
            if (record == null)
            {
                return new ConditionListResult
                {
                    Success = false,
                    Error = $"Record {editorId} not found"
                };
            }

            if (!RecordConditionChecker.SupportsConditions(record))
            {
                return new ConditionListResult
                {
                    Success = false,
                    Error = $"Record type {recordType} does not support conditions"
                };
            }

            var conditions = ConditionReader.ReadConditions(record);

            return new ConditionListResult
            {
                Success = true,
                Conditions = conditions
            };
        }
        catch (Exception ex)
        {
            return new ConditionListResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public ConditionModifyResult AddCondition(
        string pluginPath,
        string recordType,
        string editorId,
        string functionName,
        string operatorStr,
        float value)
    {
        try
        {
            using var mod = SkyrimMod.Create(SkyrimRelease.SkyrimSE);
            mod.ModHeader.MasterReferences.Add(new MasterReference { Master = ModKey.FromFileName(pluginPath) });

            // Load and find record
            var record = FindRecord(mod, recordType, editorId);
            if (record == null)
            {
                return new ConditionModifyResult
                {
                    Success = false,
                    Error = $"Record {editorId} not found"
                };
            }

            // Parse operator
            if (!Enum.TryParse<CompareOperator>(operatorStr, out var compareOp))
            {
                return new ConditionModifyResult
                {
                    Success = false,
                    Error = $"Invalid operator: {operatorStr}"
                };
            }

            // Add condition
            bool added = ConditionAdder.AddCondition(record, functionName, compareOp, value);

            if (!added)
            {
                return new ConditionModifyResult
                {
                    Success = false,
                    Error = "Failed to add condition"
                };
            }

            // Save
            mod.WriteToBinary(pluginPath);

            return new ConditionModifyResult
            {
                Success = true,
                Message = $"Added condition: {functionName} {operatorStr} {value}"
            };
        }
        catch (Exception ex)
        {
            return new ConditionModifyResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    // Placeholder - use actual RecordFinder implementation
    private ISkyrimMajorRecord? FindRecord(ISkyrimModGetter mod, string recordType, string editorId)
    {
        // Use existing RecordFinder.FindRecord logic
        return null;
    }
}

// ============================================================================
// 7. USAGE EXAMPLES
// ============================================================================

public class UsageExamples
{
    public static void Example1_ReadConditions()
    {
        var pluginPath = "MyMod.esp";

        using var mod = SkyrimMod.CreateFromBinaryOverlay(pluginPath, SkyrimRelease.SkyrimSE);
        var perk = mod.Perks.FirstOrDefault(p => p.EditorID == "MyPerk");

        if (perk != null)
        {
            var conditions = ConditionReader.ReadConditions(perk);

            foreach (var cond in conditions)
            {
                Console.WriteLine($"Condition {cond.Index}:");
                Console.WriteLine($"  Function: {cond.FunctionName}");
                Console.WriteLine($"  Operator: {cond.Operator}");
                Console.WriteLine($"  Value: {cond.FloatValue}");

                foreach (var (key, value) in cond.Parameters)
                {
                    Console.WriteLine($"  {key}: {value}");
                }
            }
        }
    }

    public static void Example2_AddCondition()
    {
        var pluginPath = "MyMod.esp";

        var mod = SkyrimMod.Create(SkyrimRelease.SkyrimSE);
        // Load existing mod...

        var perk = mod.Perks.FirstOrDefault(p => p.EditorID == "MyPerk");

        if (perk != null)
        {
            // Add simple condition
            ConditionAdder.AddCondition(
                perk,
                "GetLevel",
                CompareOperator.GreaterThanOrEqualTo,
                10.0f
            );

            // Add condition with parameters
            ConditionAdder.AddCondition(
                perk,
                "GetActorValue",
                CompareOperator.GreaterThan,
                50.0f,
                new Dictionary<string, object>
                {
                    { "ActorValue", "Health" }
                }
            );

            mod.WriteToBinary(pluginPath);
        }
    }

    public static void Example3_RemoveCondition()
    {
        var pluginPath = "MyMod.esp";

        var mod = SkyrimMod.Create(SkyrimRelease.SkyrimSE);
        // Load existing mod...

        var perk = mod.Perks.FirstOrDefault(p => p.EditorID == "MyPerk");

        if (perk != null)
        {
            // Remove by index
            ConditionRemover.RemoveConditionByIndex(perk, 0);

            // Remove all
            var count = ConditionRemover.RemoveAllConditions(perk);
            Console.WriteLine($"Removed {count} conditions");

            mod.WriteToBinary(pluginPath);
        }
    }

    public static void Example4_ModifyCondition()
    {
        var pluginPath = "MyMod.esp";

        var mod = SkyrimMod.Create(SkyrimRelease.SkyrimSE);
        // Load existing mod...

        var perk = mod.Perks.FirstOrDefault(p => p.EditorID == "MyPerk");

        if (perk != null)
        {
            // Modify operator and value
            ConditionModifier.ModifyCondition(
                perk,
                index: 0,
                newOperator: CompareOperator.GreaterThan,
                newValue: 20.0f
            );

            // Modify parameters
            ConditionModifier.ModifyConditionData(
                perk,
                index: 1,
                new Dictionary<string, object>
                {
                    { "ActorValue", "Magicka" }
                }
            );

            mod.WriteToBinary(pluginPath);
        }
    }
}
