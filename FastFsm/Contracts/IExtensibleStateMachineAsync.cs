using System;

namespace FastFsm.Contracts;

/// <summary>
/// Asynchronous extensible state machine interface
/// </summary>
public interface IExtensibleStateMachineAsync<TState, TTrigger> :
    IStateMachineAsync<TState, TTrigger>,
    IExtensibleStateMachine
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
}