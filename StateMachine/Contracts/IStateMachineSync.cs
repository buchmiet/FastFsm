using System;
using System.Collections.Generic;

namespace StateMachine.Contracts;

/// <summary>
/// Synchronous state machine interface for enum-based states and triggers
/// </summary>
public interface IStateMachineSync<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Current state of the machine
    /// </summary>
    TState CurrentState { get; }
    
    /// <summary>
    /// Indicates whether the state machine has been started
    /// </summary>
    bool IsStarted { get; }
    
    /// <summary>
    /// Starts the state machine and executes initial OnEntry if present
    /// </summary>
    void Start();
    
    /// <summary>
    /// Try to fire a trigger. Returns true if transition occurred.
    /// </summary>
    bool TryFire(TTrigger trigger, object? payload = null);
    
    /// <summary>
    /// Fire a trigger. Throws if transition is not valid.
    /// </summary>
    void Fire(TTrigger trigger, object? payload = null);
    
    /// <summary>
    /// Check if a trigger can be fired from current state
    /// </summary>
    bool CanFire(TTrigger trigger);
    
    /// <summary>
    /// Get all triggers that can be fired from current state
    /// </summary>
    IReadOnlyList<TTrigger> GetPermittedTriggers();
    
    /// <summary>
    /// Checks if the given state is in the active path (HSM support)
    /// For non-hierarchical machines, returns true only if state equals CurrentState
    /// </summary>
    bool IsIn(TState state);
    
    /// <summary>
    /// Gets the active state path from root to the current leaf state (HSM support)
    /// For non-hierarchical machines, returns single-element list with CurrentState
    /// </summary>
    IReadOnlyList<TState> GetActivePath();
}