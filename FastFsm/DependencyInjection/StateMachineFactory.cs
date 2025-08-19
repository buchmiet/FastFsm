#if FSM_DI_ENABLED

using System;
using Microsoft.Extensions.DependencyInjection;
using StateMachine.Contracts;

namespace StateMachine.DependencyInjection;

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
    public TInterface Create(TState initialState)
    {
        // Używamy ActivatorUtilities zamiast Activator.CreateInstance
        // To działa z konstruktorami z parametrami i jest AOT-friendly
        return ActivatorUtilities.CreateInstance<TImplementation>(serviceProvider, initialState);
    }
    
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