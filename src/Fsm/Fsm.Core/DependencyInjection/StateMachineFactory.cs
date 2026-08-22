#if FSM_DI_ENABLED

using System;
using Microsoft.Extensions.DependencyInjection;
using FastFsm.Contracts;

namespace FastFsm.DependencyInjection;

/// <summary>
/// Generated factory that selects appropriate variant
/// </summary>
public class StateMachineFactory<TInterface, TImplementation, TState, TTrigger>(IServiceProvider serviceProvider)
    : IStateMachineFactory<TInterface, TState, TTrigger>
    where TInterface : class
    where TImplementation : class, TInterface
    where TState : unmanaged, Enum
    where TTrigger : unmanaged, Enum
{
    public TInterface Create(TState initialState) =>
        // Use ActivatorUtilities instead of Activator.CreateInstance
        // Works with parameterized constructors and is AOT-friendly
        ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider, initialState);

    public TInterface CreateStarted(TState initialState)
    {
        var machine = Create(initialState);
        // Try to call Start() if it's a sync machine
        if (machine is IStateMachineSync<TState, TTrigger> syncMachine)
        {
            syncMachine.Start();
        }
        // For async machines, StartAsync() must be called separately
        return machine;
    }
}
#endif
