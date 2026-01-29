namespace SpookysAutomod.Core.Models;

/// <summary>
/// Information about a single condition
/// </summary>
public class ConditionInfo
{
    /// <summary>
    /// The condition function name (e.g., "GetActorValue", "IsInCombat")
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// The value to compare against
    /// </summary>
    public float ComparisonValue { get; set; }

    /// <summary>
    /// The comparison operator (e.g., "==", ">=", "<")
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// First parameter (FormKey or number as string)
    /// </summary>
    public string? ParameterA { get; set; }

    /// <summary>
    /// Second parameter (FormKey or number as string)
    /// </summary>
    public string? ParameterB { get; set; }

    /// <summary>
    /// Condition flags (e.g., "OR", "UseGlobal")
    /// </summary>
    public string Flags { get; set; } = string.Empty;

    /// <summary>
    /// What the condition runs on (Subject, Target, Reference, etc.)
    /// </summary>
    public string RunOn { get; set; } = string.Empty;
}
