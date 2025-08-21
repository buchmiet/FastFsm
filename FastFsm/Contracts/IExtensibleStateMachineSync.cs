using System;

namespace FastFsm.Contracts;

/// <summary>
/// Synchronous extensible state machine interface
/// </summary>
public interface IExtensibleStateMachineSync<TState, TTrigger> :
    IStateMachineSync<TState, TTrigger>,
    IExtensibleStateMachine
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
}