using System;

namespace StateMachine.Contracts;

/// <summary>
/// Factory interface for creating state machines
/// </summary>
public interface IStateMachineFactory<out TStateMachine, in TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    TStateMachine Create(TState initialState);
    TStateMachine CreateStarted(TState initialState);
}