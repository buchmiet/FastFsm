using System;
using FastFsm.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FastFsm.Observability.DependencyInjection;

public static class FsmObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddFastFsmObservability<TState, TTrigger>(
        this IServiceCollection services,
        Action<FastFsmObservabilityOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new FastFsmObservabilityOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.AddStateMachineObservabilityExtension<TState, TTrigger>(lifetime);

        return services;
    }

    public static IServiceCollection AddStateMachineObservabilityExtension<TState, TTrigger>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(
            typeof(IStateMachineExtension<TState, TTrigger>),
            typeof(ObservabilityExtension<TState, TTrigger>),
            lifetime));

        return services;
    }

    public static IServiceCollection AddStateMachineExtension<TState, TTrigger, TExtension>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
        where TExtension : class, IStateMachineExtension<TState, TTrigger>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(
            typeof(IStateMachineExtension<TState, TTrigger>),
            typeof(TExtension),
            lifetime));

        return services;
    }
}
