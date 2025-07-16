using System;

namespace StateMachine.Contracts;

/// <summary>
/// Factory interface for creating state machines with typed payload
/// </summary>
public interface IStateMachineWithPayloadFactory<out TStateMachine, in TState, TTrigger, TPayload>
    where TStateMachine : IStateMachineWithPayload<TState, TTrigger, TPayload>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    TStateMachine Create(TState initialState);
    TStateMachine Create(TState initialState, TPayload defaultPayload);
}