using System;

namespace StateMachine.Contracts;

/// <summary>
/// State machine interface with typed payload support
/// </summary>
public interface IStateMachineWithPayload<TState, TTrigger, TPayload> : IStateMachine<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Try to fire a trigger with typed payload
    /// </summary>
    bool TryFire(TTrigger trigger, TPayload payload);
    
    /// <summary>
    /// Fire a trigger with typed payload
    /// </summary>
    void Fire(TTrigger trigger, TPayload payload);
    
    /// <summary>
    /// Check if trigger can fire with given payload
    /// </summary>
    bool CanFire(TTrigger trigger, TPayload payload);
}