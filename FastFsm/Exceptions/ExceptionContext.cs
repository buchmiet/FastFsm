using System;

namespace FastFsm.Exceptions;

/// <summary>
/// Provides context information about an exception that occurred during a state transition.
/// </summary>
/// <typeparam name="TState">The type of state enum.</typeparam>
/// <typeparam name="TTrigger">The type of trigger enum.</typeparam>
public readonly struct ExceptionContext<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// The state from which the transition was initiated.
    /// </summary>
    public TState From { get; }
    
    /// <summary>
    /// The target state of the transition.
    /// </summary>
    public TState To { get; }
    
    /// <summary>
    /// The trigger that initiated the transition.
    /// </summary>
    public TTrigger Trigger { get; }
    
    /// <summary>
    /// The exception that was thrown during the transition.
    /// </summary>
    public Exception Exception { get; }
    
    /// <summary>
    /// The stage of the transition where the exception occurred.
    /// </summary>
    public TransitionStage Stage { get; }
    
    /// <summary>
    /// Indicates whether the state has already been changed when the exception occurred.
    /// True for OnEntry and Action stages, false for Guard and OnExit.
    /// </summary>
    public bool StateAlreadyChanged { get; }
    
    /// <summary>
    /// Initializes a new instance of the ExceptionContext struct.
    /// </summary>
    public ExceptionContext(
        TState from, 
        TState to, 
        TTrigger trigger, 
        Exception exception, 
        TransitionStage stage,
        bool stateAlreadyChanged)
    {
        From = from;
        To = to;
        Trigger = trigger;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Stage = stage;
        StateAlreadyChanged = stateAlreadyChanged;
    }
}