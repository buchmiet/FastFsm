using System.Collections.Generic;

namespace Generator.Planning;

/// <summary>
/// Represents a complete plan for executing a transition
/// </summary>
internal readonly struct TransitionPlan
{
    /// <summary>
    /// Whether this is an internal transition (no state change)
    /// </summary>
    public bool IsInternal { get; }
    
    /// <summary>
    /// Index of the source state
    /// </summary>
    public int FromStateIndex { get; }
    
    /// <summary>
    /// Index of the target state
    /// </summary>
    public int ToStateIndex { get; }
    
    /// <summary>
    /// Index of the Lowest Common Ancestor in hierarchy (-1 for flat machines)
    /// </summary>
    public int LcaIndex { get; }
    
    /// <summary>
    /// Ordered list of steps to execute this transition
    /// </summary>
    public IReadOnlyList<PlanStep> Steps { get; }
    
    /// <summary>
    /// Initializes a new instance of the TransitionPlan struct.
    /// </summary>
    public TransitionPlan(
        bool isInternal,
        int fromStateIndex,
        int toStateIndex,
        int lcaIndex,
        IReadOnlyList<PlanStep> steps)
    {
        IsInternal = isInternal;
        FromStateIndex = fromStateIndex;
        ToStateIndex = toStateIndex;
        LcaIndex = lcaIndex;
        Steps = steps ?? new List<PlanStep>();
    }
}