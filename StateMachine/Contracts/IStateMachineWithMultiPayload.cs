using System;

namespace StateMachine.Contracts;

/// <summary>
/// State machine with per-trigger payload types
/// </summary>
public interface IStateMachineWithMultiPayload<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Try to fire with dynamic payload type checking
    /// </summary>
    bool TryFire<TPayload>(TTrigger trigger, TPayload payload);
    
    /// <summary>
    /// Fire with dynamic payload type checking
    /// </summary>
    void Fire<TPayload>(TTrigger trigger, TPayload payload);
}