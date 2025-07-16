using System;
using System.Collections.Generic;

namespace StateMachine.Contracts;

/// <summary>
/// State machine that supports extensions
/// </summary>
public interface IExtensibleStateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    /// <summary>
    /// Currently registered extensions
    /// </summary>
    IReadOnlyList<IStateMachineExtension> Extensions { get; }
    
    /// <summary>
    /// Add an extension at runtime
    /// </summary>
    void AddExtension(IStateMachineExtension extension);
    
    /// <summary>
    /// Remove an extension at runtime
    /// </summary>
    bool RemoveExtension(IStateMachineExtension extension);
}
