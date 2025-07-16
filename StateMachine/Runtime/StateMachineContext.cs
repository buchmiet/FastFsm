using System;
using StateMachine.Contracts;

namespace StateMachine.Runtime;

/// <summary>
/// Concrete implementation of state machine context
/// </summary>
public readonly struct StateMachineContext<TState, TTrigger>(  string instanceId, TState fromState, TTrigger trigger, TState toState,  object? payload = null)
: IStateMachineContext<TState, TTrigger>, IStateSnapshot
where TState : unmanaged, Enum
where TTrigger : unmanaged, Enum
{
public string InstanceId { get; } = instanceId;
public DateTime Timestamp { get; } = DateTime.UtcNow;
public TState FromState { get; } = fromState;
public TTrigger Trigger { get; } = trigger;
public TState ToState { get; } = toState;
public object? Payload { get; } = payload;

object IStateSnapshot.FromState => FromState;
object IStateSnapshot.Trigger => Trigger;
object IStateSnapshot.ToState => ToState;
}

/// <summary>
/// Context with typed payload
/// </summary>
public readonly struct StateMachineContext<TState, TTrigger, TPayload>(  string instanceId, TState fromState, TTrigger trigger, TState toState, TPayload payload)
: IStateMachineContext<TState, TTrigger>, IStateSnapshot
where TState : unmanaged, Enum
where TTrigger : unmanaged, Enum
{
public string InstanceId { get; } = instanceId;
public DateTime Timestamp { get; } = DateTime.UtcNow;
public TState FromState { get; } = fromState;
public TTrigger Trigger { get; } = trigger;
public TState ToState { get; } = toState;
public TPayload TypedPayload { get; } = payload;

object? IStateMachineContext<TState, TTrigger>.Payload => TypedPayload;

// Explicit implementation of IStateSnapshot
object IStateSnapshot.FromState => FromState;
object IStateSnapshot.Trigger => Trigger;
object IStateSnapshot.ToState => ToState;
}