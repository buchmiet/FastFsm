

using System;

// Added for Enum constraint

namespace StateMachine.Contracts;

/// <summary>
/// Builder interface for compile-time configuration validation
/// </summary>
public interface IStateMachineBuilder<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Build and validate the state machine configuration
    /// </summary>
    IStateMachine<TState, TTrigger> Build(TState initialState);
}
