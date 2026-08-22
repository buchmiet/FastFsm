using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tests.Logging.TestHelpers;

public static class StateMachineWrapperFactoryComplete
{
    public enum ApiType { Fluent, Legacy }

    private static readonly Dictionary<string, Func<ApiType, string?, ILogger?, IStateMachineTestWrapper>> _factory = 
        new(StringComparer.Ordinal)
    {
        // All 16 machines
        ["PureStateMachine"] = CreatePure,
        ["BasicStateMachine"] = CreateBasic,
        ["PayloadStateMachine"] = CreatePayload,
        ["ExtensionsStateMachine"] = CreateExtensions,
        ["FullStateMachine"] = CreateFull,
        ["MultiPayloadStateMachine"] = CreateMultiPayload,
        ["LifecycleMachine"] = CreateLifecycle,
        ["AsyncLifecycleMachine"] = CreateAsyncLifecycle,
        ["InternalTransitionMachine"] = CreateInternalTransition,
        ["StructStateMachine"] = CreateStruct,
        ["InitialOnEntryStateMachineActions"] = CreateInitialOnEntry,
        ["FullMultiPayloadMachine"] = CreateFullMultiPayload,
        ["ExampleStateMachine"] = CreateExample,
        ["GuardedStateMachine"] = CreateGuarded,
        ["ExtensibleMachine"] = CreateExtensible,
        ["HsmMachine"] = CreateHsm,
    };

    public static IStateMachineTestWrapper Create(string machineType, ApiType apiType, string? initialStateName, ILogger? logger = null)
    {
        if (!_factory.TryGetValue(machineType, out var f))
            throw new NotSupportedException($"Machine type '{machineType}' not supported. Available: {string.Join(", ", _factory.Keys)}");
        return f(apiType, initialStateName, logger);
    }

    // Factory implementations for all 16 machines
    private static IStateMachineTestWrapper CreatePure(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new PureStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new PureStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateBasic(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new BasicStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new BasicStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreatePayload(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new PayloadStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new PayloadStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateExtensions(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new ExtensionsStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new ExtensionsStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateFull(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new FullStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new FullStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateMultiPayload(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new MultiPayloadStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new MultiPayloadStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateLifecycle(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new LifecycleMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new LifecycleMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateAsyncLifecycle(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new AsyncLifecycleMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new AsyncLifecycleMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateInternalTransition(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new InternalTransitionMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new InternalTransitionMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateStruct(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new StructStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new StructStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateInitialOnEntry(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new InitialOnEntryStateMachineActionsLegacyWrapper(initial, logger),
        ApiType.Fluent => new InitialOnEntryStateMachineActionsFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateFullMultiPayload(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new FullMultiPayloadMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new FullMultiPayloadMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateExample(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new ExampleStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new ExampleStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateGuarded(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new GuardedStateMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new GuardedStateMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateExtensible(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new ExtensibleMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new ExtensibleMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    private static IStateMachineTestWrapper CreateHsm(ApiType api, string? initial, ILogger? logger) => api switch
    {
        ApiType.Legacy => new HsmMachineLegacyWrapper(initial, logger),
        ApiType.Fluent => new HsmMachineFluentWrapper(initial, logger),
        _ => throw new ArgumentOutOfRangeException()
    };

    // Helper methods
    public static Type GetStateEnumType(string machine, ApiType api) =>
        MachineTypeRegistry.GetStateType(machine, api == ApiType.Fluent ? MachineTypeRegistry.Api.Fluent : MachineTypeRegistry.Api.Legacy);
        
    public static Type GetTriggerEnumType(string machine, ApiType api) =>
        MachineTypeRegistry.GetTriggerType(machine, api == ApiType.Fluent ? MachineTypeRegistry.Api.Fluent : MachineTypeRegistry.Api.Legacy);

    public static object GetStateEnum(string machine, ApiType api, string name) => 
        Enum.Parse(GetStateEnumType(machine, api), name, ignoreCase: false);
        
    public static object GetTriggerEnum(string machine, ApiType api, string name) => 
        Enum.Parse(GetTriggerEnumType(machine, api), name, ignoreCase: false);
}