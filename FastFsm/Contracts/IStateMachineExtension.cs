namespace StateMachine.Contracts;

/// <summary>
/// Extension interface for adding cross-cutting concerns to state machines
/// </summary>
public interface IStateMachineExtension
{
    /// <summary>
    /// Called before a transition is attempted
    /// </summary>
    void OnBeforeTransition<TContext>(TContext context) 
        where TContext : IStateMachineContext;
    
    /// <summary>
    /// Called after a transition completes
    /// </summary>
    void OnAfterTransition<TContext>(TContext context, bool success) 
        where TContext : IStateMachineContext;
    
    /// <summary>
    /// Called when guard evaluation starts
    /// </summary>
    void OnGuardEvaluation<TContext>(TContext context, string guardName) 
        where TContext : IStateMachineContext;
    
    /// <summary>
    /// Called when guard evaluation completes
    /// </summary>
    void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) 
        where TContext : IStateMachineContext;
}