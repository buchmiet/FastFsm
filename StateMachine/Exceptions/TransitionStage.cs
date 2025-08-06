namespace StateMachine.Exceptions;

/// <summary>
/// Identifies the stage of transition where an exception occurred.
/// </summary>
public enum TransitionStage
{
    /// <summary>
    /// Exception occurred in guard evaluation.
    /// </summary>
    Guard,
    
    /// <summary>
    /// Exception occurred in OnExit callback.
    /// </summary>
    OnExit,
    
    /// <summary>
    /// Exception occurred in OnEntry callback.
    /// </summary>
    OnEntry,
    
    /// <summary>
    /// Exception occurred in transition action.
    /// </summary>
    Action
}