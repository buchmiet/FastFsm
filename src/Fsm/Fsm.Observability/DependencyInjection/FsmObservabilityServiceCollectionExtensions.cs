using System;
using FastFsm.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastFsm.Observability.DependencyInjection;

public static class FsmObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddFastFsmObservability<TState, TTrigger>(
        this IServiceCollection services,
        Action<FastFsmObservabilityOptions.Builder>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = FastFsmObservabilityOptions.Create(configure);

        services.Add(new ServiceDescriptor(
            typeof(ObservabilityExtension<TState, TTrigger>),
            sp => CreateExtension<TState, TTrigger>(sp, options),
            lifetime));

        services.Add(new ServiceDescriptor(
            typeof(IStateMachineExtension<TState, TTrigger>),
            sp => sp.GetRequiredService<ObservabilityExtension<TState, TTrigger>>(),
            lifetime));

        return services;
    }

    public static IServiceCollection AddStateMachineObservabilityExtension<TState, TTrigger>(
        this IServiceCollection services,
        FastFsmObservabilityOptions options,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.Add(new ServiceDescriptor(
            typeof(ObservabilityExtension<TState, TTrigger>),
            sp => CreateExtension<TState, TTrigger>(sp, options),
            lifetime));

        services.Add(new ServiceDescriptor(
            typeof(IStateMachineExtension<TState, TTrigger>),
            sp => sp.GetRequiredService<ObservabilityExtension<TState, TTrigger>>(),
            lifetime));

        return services;
    }

    private static ObservabilityExtension<TState, TTrigger> CreateExtension<TState, TTrigger>(
        IServiceProvider serviceProvider,
        FastFsmObservabilityOptions options)
        where TState : unmanaged, Enum
        where TTrigger : unmanaged, Enum
    {
        ILogger<ObservabilityExtension<TState, TTrigger>>? logger = null;
        if (options.Logging)
        {
            logger = serviceProvider.GetService<ILogger<ObservabilityExtension<TState, TTrigger>>>();
        }

        IObservabilityEventSink? eventSink = null;
        if (options.EventStream)
        {
            eventSink = serviceProvider.GetService<IObservabilityEventSink>();
        }

        return new ObservabilityExtension<TState, TTrigger>(options, logger, eventSink);
    }
}
