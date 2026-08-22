using System;
using Microsoft.Extensions.DependencyInjection;
using FastFsm.DependencyInjection;
using Tests.DependencyInjection.TestMachines;

namespace Tests.DependencyInjection
{
    public static class ServiceCollectionTestExtensions
    {
        // Pure
        public static IServiceCollection AddPureTestMachine(this IServiceCollection services, TestState initialState, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.ConfigureStateMachineInitialState<TestState>(_ => initialState);
            return services.AddStateMachine<IPureTestMachine, PureTestMachine, TestState, TestTrigger>(lifetime);
        }

        // Basic
        public static IServiceCollection AddBasicTestMachine(this IServiceCollection services, TestState initialState, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.ConfigureStateMachineInitialState<TestState>(_ => initialState);
            return services.AddStateMachine<IBasicTestMachine, BasicTestMachine, TestState, TestTrigger>(lifetime);
        }

        public static IServiceCollection AddBasicTestMachine(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddStateMachine<IBasicTestMachine, BasicTestMachine, TestState, TestTrigger>(lifetime);

        // Guarded (extensions variant)
        public static IServiceCollection AddGuardedTestMachine(this IServiceCollection services, TestState initialState, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.ConfigureStateMachineInitialState<TestState>(_ => initialState);
            return services.AddStateMachine<IGuardedTestMachine, GuardedTestMachine, TestState, TestTrigger>(lifetime);
        }

        public static IServiceCollection AddGuardedTestMachine(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddStateMachine<IGuardedTestMachine, GuardedTestMachine, TestState, TestTrigger>(lifetime);

        public static IServiceCollection AddPureTestMachine(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddStateMachine<IPureTestMachine, PureTestMachine, TestState, TestTrigger>(lifetime);

        public static IServiceCollection AddPureTestMachine(this IServiceCollection services, Func<IServiceProvider, TestState> initialFactory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.ConfigureStateMachineInitialState(initialFactory);
            return services.AddStateMachine<IPureTestMachine, PureTestMachine, TestState, TestTrigger>(lifetime);
        }

        // Extensions
        public static IServiceCollection AddExtensionsTestMachine(this IServiceCollection services, TestState initialState, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.ConfigureStateMachineInitialState<TestState>(_ => initialState);
            return services.AddStateMachine<IExtensionsTestMachine, ExtensionsTestMachine, TestState, TestTrigger>(lifetime);
        }

        public static IServiceCollection AddExtensionsTestMachine(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddStateMachine<IExtensionsTestMachine, ExtensionsTestMachine, TestState, TestTrigger>(lifetime);

        // Full (Payload + Extensions)
        public static IServiceCollection AddFullTestMachine(this IServiceCollection services, TestState initialState, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            services.ConfigureStateMachineInitialState<TestState>(_ => initialState);
            return services.AddStateMachine<IFullTestMachine, FullTestMachine, TestState, TestTrigger>(lifetime);
        }

        public static IServiceCollection AddFullTestMachine(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton) => services.AddStateMachine<IFullTestMachine, FullTestMachine, TestState, TestTrigger>(lifetime);
    }
}
