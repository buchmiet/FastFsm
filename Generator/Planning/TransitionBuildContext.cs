using Generator.Model;
using System.Collections.Generic;

namespace Generator.Planning;

/// <summary>
/// Context for building a transition plan
/// </summary>
public class TransitionBuildContext
{
    /// <summary>
    /// The state machine model
    /// </summary>
    public StateMachineModel Model { get; }
    
    /// <summary>
    /// The transition being planned
    /// </summary>
    public TransitionModel Transition { get; }
    
    /// <summary>
    /// Current state index (for runtime resolution)
    /// </summary>
    public int CurrentStateIndex { get; }
    
    /// <summary>
    /// Whether this is for async variant
    /// </summary>
    public bool IsAsyncVariant { get; }
    
    /// <summary>
    /// Whether the machine has payload support
    /// </summary>
    public bool HasPayload { get; }
    
    /// <summary>
    /// Ordered list of all state names
    /// </summary>
    public IReadOnlyList<string> AllStates { get; }
    
    /// <summary>
    /// Parent indices array (from hierarchy)
    /// </summary>
    public int[] ParentIndices { get; }
    
    /// <summary>
    /// Depth array (from hierarchy)
    /// </summary>
    public int[] Depths { get; }
    
    /// <summary>
    /// Initial child indices (from hierarchy)
    /// </summary>
    public int[] InitialChildIndices { get; }
    
    /// <summary>
    /// History modes for states
    /// </summary>
    public HistoryMode[] HistoryModes { get; }
    
    public TransitionBuildContext(
        StateMachineModel model,
        TransitionModel transition,
        int currentStateIndex,
        bool isAsyncVariant,
        bool hasPayload,
        IReadOnlyList<string> allStates,
        int[]? parentIndices = null,
        int[]? depths = null,
        int[]? initialChildIndices = null,
        HistoryMode[]? historyModes = null)
    {
        Model = model;
        Transition = transition;
        CurrentStateIndex = currentStateIndex;
        IsAsyncVariant = isAsyncVariant;
        HasPayload = hasPayload;
        AllStates = allStates;
        ParentIndices = parentIndices ?? new int[0];
        Depths = depths ?? new int[0];
        InitialChildIndices = initialChildIndices ?? new int[0];
        HistoryModes = historyModes ?? new HistoryMode[0];
    }
}