namespace StateMachine.Exceptions;

/// <summary>
/// Specifies how to handle exceptions in state machine callbacks.
/// </summary>
public enum ExceptionDirective
{
    /// <summary>
    /// Propagate the exception to the caller.
    /// </summary>
    Propagate = 0,
    
    /// <summary>
    /// Swallow the exception and continue execution.
    /// </summary>
    Continue = 1
}