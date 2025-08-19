using System;

namespace StateMachine.Contracts;

/// <summary>
/// Generic context with state and trigger information
/// </summary>
public interface IStateMachineContext<TState, TTrigger> : IStateMachineContext
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    TState FromState { get; }
    TTrigger Trigger { get; }
    TState ToState { get; }
    object? Payload { get; }
}

/// <summary>
/// Context passed to extensions
/// </summary>
public interface IStateMachineContext
{
    /// <summary>
    /// State machine instance ID
    /// </summary>
    string InstanceId { get; }

    /// <summary>
    /// Timestamp of the operation
    /// </summary>
    DateTime Timestamp { get; }
}