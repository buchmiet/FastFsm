namespace Generator.Planning;

/// <summary>
/// Represents a single step in a transition plan
/// </summary>
public readonly struct PlanStep
{
    /// <summary>
    /// The type of this step
    /// </summary>
    public PlanStepKind Kind { get; }
    
    /// <summary>
    /// Index of the state involved in this step (-1 if not applicable)
    /// </summary>
    public int StateIndex { get; }
    
    /// <summary>
    /// Index of the parent state (-1 if not applicable)
    /// </summary>
    public int ParentIndex { get; }
    
    /// <summary>
    /// Index of the trigger (-1 if not applicable)
    /// </summary>
    public int TriggerIndex { get; }
    
    /// <summary>
    /// Name of the guard method (null if not applicable)
    /// </summary>
    public string? GuardMethod { get; }
    
    /// <summary>
    /// Name of the action method (null if not applicable)
    /// </summary>
    public string? ActionMethod { get; }
    
    /// <summary>
    /// Log template for logging steps (null if not applicable)
    /// </summary>
    public string? LogTemplate { get; }
    
    /// <summary>
    /// Whether the action is async
    /// </summary>
    public bool IsAsyncAction { get; }
    
    /// <summary>
    /// Whether this is an internal transition
    /// </summary>
    public bool IsInternal { get; }
    
    /// <summary>
    /// Whether to use deep history
    /// </summary>
    public bool UseDeepHistory { get; }
    
    /// <summary>
    /// Whether the transition has payload
    /// </summary>
    public bool HasPayload { get; }
    
    /// <summary>
    /// State name for logging/debugging
    /// </summary>
    public string? StateName { get; }
    
    /// <summary>
    /// OnEntry method name
    /// </summary>
    public string? OnEntryMethod { get; }
    
    /// <summary>
    /// OnExit method name
    /// </summary>
    public string? OnExitMethod { get; }
    
    public PlanStep(
        PlanStepKind kind,
        int stateIndex = -1,
        int parentIndex = -1,
        int triggerIndex = -1,
        string? guardMethod = null,
        string? actionMethod = null,
        string? logTemplate = null,
        bool isAsyncAction = false,
        bool isInternal = false,
        bool useDeepHistory = false,
        bool hasPayload = false,
        string? stateName = null,
        string? onEntryMethod = null,
        string? onExitMethod = null)
    {
        Kind = kind;
        StateIndex = stateIndex;
        ParentIndex = parentIndex;
        TriggerIndex = triggerIndex;
        GuardMethod = guardMethod;
        ActionMethod = actionMethod;
        LogTemplate = logTemplate;
        IsAsyncAction = isAsyncAction;
        IsInternal = isInternal;
        UseDeepHistory = useDeepHistory;
        HasPayload = hasPayload;
        StateName = stateName;
        OnEntryMethod = onEntryMethod;
        OnExitMethod = onExitMethod;
    }
}