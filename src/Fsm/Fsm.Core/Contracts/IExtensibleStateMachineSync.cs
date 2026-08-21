using System;

namespace FastFsm.Contracts;

/// <summary>
/// Synchronous extensible state machine interface
/// </summary>
public interface IExtensibleStateMachineSync<TState, TTrigger> :
    IStateMachineSync<TState, TTrigger>,
    IExtensibleStateMachine<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
}