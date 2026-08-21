using System;
using System.Collections.Generic;

namespace FastFsm.Contracts;

public interface IStateMachineIdentity
{
    Guid InstanceId { get; }
}

/// <summary>
/// Exposes the extension set of a state machine.
/// </summary>
public interface IExtensibleStateMachine<TState, TTrigger> : IStateMachineIdentity
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    IReadOnlyList<IStateMachineExtension<TState, TTrigger>> Extensions { get; }

    void AddExtension(IStateMachineExtension<TState, TTrigger> extension);

    bool RemoveExtension(IStateMachineExtension<TState, TTrigger> extension);
}