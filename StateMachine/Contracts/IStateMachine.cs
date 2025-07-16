

using System;
using System.Collections.Generic;

namespace StateMachine.Contracts;

/// <summary>
/// High-performance state machine interface for enum-based states and triggers
/// </summary>
public interface IStateMachine<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Current state of the machine
    /// </summary>
    TState CurrentState { get; }
    
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
}
