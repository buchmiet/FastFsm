namespace Generator.Planning;

/// <summary>
/// Defines the type of step in a transition plan
/// </summary>
internal enum PlanStepKind
{
    /// <summary>
    /// Exit a state (calls OnExit)
    /// </summary>
    ExitState,
    
    /// <summary>
    /// Enter a state (calls OnEntry)
    /// </summary>
    EntryState,
    
    /// <summary>
    /// Assign the current state field
    /// </summary>
    AssignState,
    
    /// <summary>
    /// Execute an internal action without state change
    /// </summary>
    InternalAction,
    
    /// <summary>
    /// Record history for a composite state
    /// </summary>
    RecordHistory,
    
    /// <summary>
    /// Log a transition or state change
    /// </summary>
    Log,
    
    /// <summary>
    /// Check a guard condition
    /// </summary>
    GuardCheck
}