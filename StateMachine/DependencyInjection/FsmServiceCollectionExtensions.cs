#if FSM_DI_ENABLED
using Microsoft.Extensions.DependencyInjection;
using StateMachine.Contracts;
using System;

namespace StateMachine.DependencyInjection;

public static class FsmServiceCollectionExtensions
{

    /// <summary>
    /// Register a fast state machine with automatic variant selection
    /// </summary>
    public static IServiceCollection AddStateMachine<TInterface, TImplementation, TState, TTrigger>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TInterface : class, IStateMachine<TState, TTrigger>
        where TImplementation : class, TInterface
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        // Register factory
        services.Add(new ServiceDescriptor(
            typeof(IStateMachineFactory<TInterface, TState, TTrigger>),
            typeof(StateMachineFactory<TInterface, TImplementation, TState, TTrigger>),
            lifetime));
            
        // Register state machine via factory
        services.Add(new ServiceDescriptor(
            typeof(TInterface),
            provider =>
            {
                var factory = provider.GetRequiredService<IStateMachineFactory<TInterface, TState, TTrigger>>();
                var initialState = GetInitialState<TState>(provider);
                return factory.Create(initialState);
            },
            lifetime));
            
        return services;
    }
    
    /// <summary>
    /// Register extensions for all state machines
    /// </summary>
    public static IServiceCollection AddStateMachineExtension<TExtension>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TExtension : class, IStateMachineExtension
    {
        services.Add(new ServiceDescriptor(
            typeof(IStateMachineExtension),
            typeof(TExtension),
            lifetime));
            
        return services;
    }
    
    /// <summary>
    /// Configure initial state provider
    /// </summary>
    public static IServiceCollection ConfigureStateMachineInitialState<TState>(
        this IServiceCollection services,
        Func<IServiceProvider, TState> initialStateFactory)
        where TState : unmanaged, Enum
    {
        services.AddSingleton<IInitialStateProvider<TState>>(
            new DelegateInitialStateProvider<TState>(initialStateFactory));
        return services;
    }
    
    private static TState GetInitialState<TState>(IServiceProvider provider)
        where TState : unmanaged, Enum
    {
        var stateProvider = provider.GetService<IInitialStateProvider<TState>>();
        return stateProvider?.GetInitialState(provider) ?? Enum.GetValues<TState>()[0];
    }
}

/// <summary>
/// Interface for providing initial state
/// </summary>
public interface IInitialStateProvider<out TState>
    where TState : unmanaged, Enum
{
    TState GetInitialState(IServiceProvider serviceProvider);
}

internal class DelegateInitialStateProvider<TState>(Func<IServiceProvider, TState> factory)
    : IInitialStateProvider<TState>
    where TState : unmanaged, Enum
{
    public TState GetInitialState(IServiceProvider serviceProvider) => factory(serviceProvider);
}
#endif