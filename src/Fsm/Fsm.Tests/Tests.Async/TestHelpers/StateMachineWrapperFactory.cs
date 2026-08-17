using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests.Async.Features.Hsm.Runtime;

namespace Tests.Async.TestHelpers
{
    public static partial class StateMachineWrapperFactory
    {
        public enum ApiType { Fluent, Legacy }

        private static Type GetStateEnumType(string machine, ApiType api) =>
            MachineTypeRegistry.GetStateType(machine, api == ApiType.Fluent ? MachineTypeRegistry.Api.Fluent : MachineTypeRegistry.Api.Legacy);
        private static Type GetTriggerEnumType(string machine, ApiType api) =>
            MachineTypeRegistry.GetTriggerType(machine, api == ApiType.Fluent ? MachineTypeRegistry.Api.Fluent : MachineTypeRegistry.Api.Legacy);

        public static object GetStateEnum(string machine, ApiType api, string name)
        { var type = GetStateEnumType(machine, api); return Enum.Parse(type, name, ignoreCase: false); }
        public static object GetTriggerEnum(string machine, ApiType api, string name)
        { var type = GetTriggerEnumType(machine, api); return Enum.Parse(type, name, ignoreCase: false); }

        private static readonly Dictionary<string, Func<ApiType, string?, IStateMachineTestWrapper>> _factory = new(StringComparer.Ordinal)
        {
            ["InitialChild"] = CreateInitialChild,
            ["ShallowHistory"] = CreateShallowHistory,
            ["DeepHistory"] = CreateDeepHistory,
            ["Internal"] = CreateInternal,
            ["Priority"] = CreatePriority,
            ["ChildOverrides"] = CreateChildOverrides,
            ["SourceOrderTie"] = CreateSourceOrderTie,
            ["Inheritance"] = CreateInheritance,

            // Payload
            ["BasicPayload"] = CreateBasicPayload,
            ["OverloadedPayload"] = CreateOverloadedPayload,
            ["ExceptionPayload"] = CreateExceptionPayload,
            ["CanFirePayload"] = CreateCanFirePayload,
            ["ConcurrentPayload"] = CreateConcurrentPayload,
            ["InitialOnEntryPayload"] = CreateInitialOnEntryPayload,
            ["MultiPayload"] = CreateMultiPayload,

            // Cancellation
            ["BasicToken"] = CreateBasicToken,
            ["OptionalToken"] = CreateOptionalToken,
            ["Cancellation"] = CreateCancellation,
            ["MixedToken"] = CreateMixedToken,

            // Exceptions
            ["OnEntryContinue"] = CreateOnEntryContinue,
            ["ActionPropagate"] = CreateActionPropagate,
            ["GuardException"] = CreateGuardException,
            ["CancellationPropagation"] = CreateCancellationPropagation,
            ["AsyncHandler"] = CreateAsyncHandler,
            ["ExceptionContextCapture"] = CreateExceptionContextCapture,

            // Extensions
            ["ExtensionsSuccess"] = CreateExtensionsSuccess,
            ["ExtensionsFail"] = CreateExtensionsFail,

            // Concurrency/Core
            ["RcMachine"] = CreateRcMachine,
            ["SimpleAsync"] = CreateSimpleAsync,

            // Alias keys mapping base machine names to existing creators
            ["InitialChildMachine"] = CreateInitialChild,
            ["ShallowHistoryMachine"] = CreateShallowHistory,
            ["DeepHistoryMachine"] = CreateDeepHistory,
            ["InternalMachine"] = CreateInternal,
            ["PriorityMachine"] = CreatePriority,
            ["ChildOverridesMachine"] = CreateChildOverrides,
            ["SourceOrderTieMachine"] = CreateSourceOrderTie,
            ["InheritanceMachine"] = CreateInheritance,

            ["BasicAsyncPayloadMachine"] = CreateBasicPayload,
            ["OverloadedAsyncMachine"] = CreateOverloadedPayload,
            ["ExceptionAsyncPayloadMachine"] = CreateExceptionPayload,
            ["CanFireAsyncPayloadMachine"] = CreateCanFirePayload,
            ["ConcurrentAsyncPayloadMachine"] = CreateConcurrentPayload,
            ["InitialOnEntryAsyncPayloadMachine"] = CreateInitialOnEntryPayload,
            ["MultiPayloadAsyncMachine"] = CreateMultiPayload,

            ["BasicTokenMachine"] = CreateBasicToken,
            ["OptionalTokenMachine"] = CreateOptionalToken,
            ["CancellationMachine"] = CreateCancellation,
            ["MixedTokenMachine"] = CreateMixedToken,

            ["OnEntryContinueMachine"] = CreateOnEntryContinue,
            ["ActionPropagateMachine"] = CreateActionPropagate,
            ["GuardExceptionMachine"] = CreateGuardException,
            ["CancellationPropagationMachine"] = CreateCancellationPropagation,
            ["AsyncHandlerMachine"] = CreateAsyncHandler,
            ["ExceptionContextCaptureMachine"] = CreateExceptionContextCapture,

            ["AsyncHookOrderMachineSuccess"] = CreateExtensionsSuccess,
            ["AsyncHookOrderMachineFail"] = CreateExtensionsFail,
            ["SimpleAsyncMachine"] = CreateSimpleAsync,
            ["RcMachine"] = CreateRcMachine,

            // New base machines
            ["TinyAsyncHsm"] = CreateTinyAsyncHsm,
            ["SpecificationComplianceMachine"] = CreateSpecificationCompliance,
            ["SimpleCancellationMachine"] = CreateSimpleCancellation,
            ["TokenMachine"] = CreateTokenMachine,
            ["PayloadMachine"] = CreatePayloadMachine,
            ["AsyncExtensionsMachine"] = CreateAsyncExtensions,
            ["ExceptionAsyncMachine"] = CreateExceptionAsync,
        };

        public static IStateMachineTestWrapper Create(string machineType, ApiType apiType, string? initialStateName)
        {
            if (!_factory.TryGetValue(machineType, out var f))
                throw new NotSupportedException($"Machine type '{machineType}' not supported");
            return f(apiType, initialStateName);
        }

        private static IStateMachineTestWrapper CreateInitialChild(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("InitialChild", typeof(AsyncInitialChildTests.S), initial);
            var s = (AsyncInitialChildTests.S)Enum.Parse(typeof(AsyncInitialChildTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.InitialChildFluentWrapper(new AsyncInitialChildTestsFluent.InitialChildMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.InitialChildLegacyWrapper(new AsyncInitialChildTests.InitialChildMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateShallowHistory(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("ShallowHistory", typeof(AsyncShallowHistoryTests.S), initial);
            var s = (AsyncShallowHistoryTests.S)Enum.Parse(typeof(AsyncShallowHistoryTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.ShallowHistoryFluentWrapper(new AsyncShallowHistoryTestsFluent.ShallowHistoryMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.ShallowHistoryLegacyWrapper(new AsyncShallowHistoryTests.ShallowHistoryMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateDeepHistory(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("DeepHistory", typeof(AsyncDeepHistoryTests.S), initial);
            var s = (AsyncDeepHistoryTests.S)Enum.Parse(typeof(AsyncDeepHistoryTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.DeepHistoryFluentWrapper(new AsyncDeepHistoryTestsFluent.DeepHistoryMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.DeepHistoryLegacyWrapper(new AsyncDeepHistoryTests.DeepHistoryMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateInternal(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("Internal", typeof(AsyncInternalTransitionTests.S), initial);
            var s = (AsyncInternalTransitionTests.S)Enum.Parse(typeof(AsyncInternalTransitionTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.InternalFluentWrapper(new AsyncInternalTransitionTestsFluent.InternalMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.InternalLegacyWrapper(new AsyncInternalTransitionTests.InternalMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreatePriority(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("Priority", typeof(AsyncResolutionOrderTests.S), initial);
            var s = (AsyncResolutionOrderTests.S)Enum.Parse(typeof(AsyncResolutionOrderTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.PriorityFluentWrapper(new AsyncResolutionOrderTestsFluent.PriorityMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.PriorityLegacyWrapper(new AsyncResolutionOrderTests.PriorityMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateChildOverrides(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("ChildOverrides", typeof(AsyncResolutionOrderTests.S), initial);
            var s = (AsyncResolutionOrderTests.S)Enum.Parse(typeof(AsyncResolutionOrderTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.ChildOverridesFluentWrapper(new AsyncResolutionOrderTestsFluent.ChildOverridesMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.ChildOverridesLegacyWrapper(new AsyncResolutionOrderTests.ChildOverridesMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateSourceOrderTie(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("SourceOrderTie", typeof(AsyncResolutionOrderTests.S), initial);
            var s = (AsyncResolutionOrderTests.S)Enum.Parse(typeof(AsyncResolutionOrderTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.SourceOrderTieFluentWrapper(new AsyncResolutionOrderTestsFluent.SourceOrderTieMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.SourceOrderTieLegacyWrapper(new AsyncResolutionOrderTests.SourceOrderTieMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateInheritance(ApiType api, string? initial)
        {
            var stateName = InitialStateResolver.Resolve("Inheritance", typeof(AsyncInheritanceAndIntrospectionTests.S), initial);
            var s = (AsyncInheritanceAndIntrospectionTests.S)Enum.Parse(typeof(AsyncInheritanceAndIntrospectionTests.S), stateName);
            return api switch
            {
                ApiType.Fluent => new HsmWrappers.InheritanceFluentWrapper(new AsyncInheritanceAndIntrospectionTestsFluent.InheritanceMachineFluentFsm(s)),
                ApiType.Legacy => new HsmWrappers.InheritanceLegacyWrapper(new AsyncInheritanceAndIntrospectionTests.InheritanceMachine(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    internal static class HsmWrappers
    {
        // Generic helpers for async-only machines
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();

        private static ApiCapabilities HsmCaps(bool internalTransitions = false) =>
            ApiCapabilities.HasAsync | ApiCapabilities.IsHierarchical | (internalTransitions ? ApiCapabilities.HasInternalTransitions : ApiCapabilities.None);

        // InitialChild
        internal sealed class InitialChildLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncInitialChildTests.InitialChildMachine _m;
            public InitialChildLegacyWrapper(AsyncInitialChildTests.InitialChildMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncInitialChildTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncInitialChildTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncInitialChildTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncInitialChildTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncInitialChildTests.T)trigger);
        }

        // TinyAsyncHsm (CompileTime)
        internal sealed class TinyLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsm _m;
            public TinyLegacy(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
        }
        internal sealed class TinyFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsmFluentFsm _m;
            public TinyFluent(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsmFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
        }
        internal sealed class InitialChildFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncInitialChildTestsFluent.InitialChildMachineFluentFsm _m;
            public InitialChildFluentWrapper(AsyncInitialChildTestsFluent.InitialChildMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncInitialChildTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncInitialChildTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncInitialChildTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncInitialChildTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncInitialChildTests.T)trigger);
        }

        // ShallowHistory
        internal sealed class ShallowHistoryLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncShallowHistoryTests.ShallowHistoryMachine _m;
            public ShallowHistoryLegacyWrapper(AsyncShallowHistoryTests.ShallowHistoryMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncShallowHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncShallowHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncShallowHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncShallowHistoryTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncShallowHistoryTests.T)trigger);
        }
        internal sealed class ShallowHistoryFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncShallowHistoryTestsFluent.ShallowHistoryMachineFluentFsm _m;
            public ShallowHistoryFluentWrapper(AsyncShallowHistoryTestsFluent.ShallowHistoryMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncShallowHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncShallowHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncShallowHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncShallowHistoryTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncShallowHistoryTests.T)trigger);
        }

        // DeepHistory
        internal sealed class DeepHistoryLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncDeepHistoryTests.DeepHistoryMachine _m;
            public DeepHistoryLegacyWrapper(AsyncDeepHistoryTests.DeepHistoryMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncDeepHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncDeepHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncDeepHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncDeepHistoryTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncDeepHistoryTests.T)trigger);
        }
        internal sealed class DeepHistoryFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncDeepHistoryTestsFluent.DeepHistoryMachineFluentFsm _m;
            public DeepHistoryFluentWrapper(AsyncDeepHistoryTestsFluent.DeepHistoryMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncDeepHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncDeepHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncDeepHistoryTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncDeepHistoryTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncDeepHistoryTests.T)trigger);
        }

        // Internal transitions
        internal sealed class InternalLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncInternalTransitionTests.InternalMachine _m;
            public InternalLegacyWrapper(AsyncInternalTransitionTests.InternalMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps(internalTransitions: true);
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncInternalTransitionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncInternalTransitionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncInternalTransitionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncInternalTransitionTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncInternalTransitionTests.T)trigger);
        }
        internal sealed class InternalFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncInternalTransitionTestsFluent.InternalMachineFluentFsm _m;
            public InternalFluentWrapper(AsyncInternalTransitionTestsFluent.InternalMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps(internalTransitions: true);
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncInternalTransitionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncInternalTransitionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncInternalTransitionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncInternalTransitionTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncInternalTransitionTests.T)trigger);
        }

        // Priority
        internal sealed class PriorityLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncResolutionOrderTests.PriorityMachine _m;
            public PriorityLegacyWrapper(AsyncResolutionOrderTests.PriorityMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger);
        }
        internal sealed class PriorityFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncResolutionOrderTestsFluent.PriorityMachineFluentFsm _m;
            public PriorityFluentWrapper(AsyncResolutionOrderTestsFluent.PriorityMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger);
        }

        // ChildOverrides
        internal sealed class ChildOverridesLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncResolutionOrderTests.ChildOverridesMachine _m;
            public ChildOverridesLegacyWrapper(AsyncResolutionOrderTests.ChildOverridesMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger);
        }
        internal sealed class ChildOverridesFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncResolutionOrderTestsFluent.ChildOverridesMachineFluentFsm _m;
            public ChildOverridesFluentWrapper(AsyncResolutionOrderTestsFluent.ChildOverridesMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger);
        }

        // SourceOrderTie
        internal sealed class SourceOrderTieLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncResolutionOrderTests.SourceOrderTieMachine _m;
            public SourceOrderTieLegacyWrapper(AsyncResolutionOrderTests.SourceOrderTieMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger);
        }
        internal sealed class SourceOrderTieFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncResolutionOrderTestsFluent.SourceOrderTieMachineFluentFsm _m;
            public SourceOrderTieFluentWrapper(AsyncResolutionOrderTestsFluent.SourceOrderTieMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncResolutionOrderTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncResolutionOrderTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncResolutionOrderTests.T)trigger);
        }

        // Inheritance
        internal sealed class InheritanceLegacyWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncInheritanceAndIntrospectionTests.InheritanceMachine _m;
            public InheritanceLegacyWrapper(AsyncInheritanceAndIntrospectionTests.InheritanceMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger);
        }
        internal sealed class InheritanceFluentWrapper : IStateMachineTestWrapper
        {
            private readonly AsyncInheritanceAndIntrospectionTestsFluent.InheritanceMachineFluentFsm _m;
            public InheritanceFluentWrapper(AsyncInheritanceAndIntrospectionTestsFluent.InheritanceMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((AsyncInheritanceAndIntrospectionTests.T)trigger);
        }
    }

    internal static class PayloadWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities CapsDefault => ApiCapabilities.HasAsync | ApiCapabilities.HasDefaultPayload;
        private static ApiCapabilities CapsMulti => ApiCapabilities.HasAsync | ApiCapabilities.HasMultiPayloads;

        // Basic
        internal sealed class BasicLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.BasicAsyncPayloadMachine _m;
            public BasicLegacy(Tests.Async.Features.Payload.BasicAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class BasicFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.BasicAsyncPayloadMachineFluentFsm _m;
            public BasicFluentWrapper(Tests.Async.Features.Payload.BasicAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // Overloaded
        internal sealed class OverloadedLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.OverloadedAsyncMachine _m;
            public OverloadedLegacy(Tests.Async.Features.Payload.OverloadedAsyncMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class OverloadedFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.OverloadedAsyncMachineFluentFsm _m;
            public OverloadedFluentWrapper(Tests.Async.Features.Payload.OverloadedAsyncMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // Exception
        internal sealed class ExceptionLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.ExceptionAsyncPayloadMachine _m;
            public ExceptionLegacy(Tests.Async.Features.Payload.ExceptionAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class ExceptionFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.ExceptionAsyncPayloadMachineFluentFsm _m;
            public ExceptionFluentWrapper(Tests.Async.Features.Payload.ExceptionAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // CanFire
        internal sealed class CanFireLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.CanFireAsyncPayloadMachine _m;
            public CanFireLegacy(Tests.Async.Features.Payload.CanFireAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, new Tests.Async.Features.Payload.ProcessPayload { Id = 1 }).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class CanFireFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.CanFireAsyncPayloadMachineFluentFsm _m;
            public CanFireFluentWrapper(Tests.Async.Features.Payload.CanFireAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, new Tests.Async.Features.Payload.ProcessPayload { Id = 1 }).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // Concurrent
        internal sealed class ConcurrentLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.ConcurrentAsyncPayloadMachine _m;
            public ConcurrentLegacy(Tests.Async.Features.Payload.ConcurrentAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault | ApiCapabilities.HasInternalTransitions;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class ConcurrentFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.ConcurrentAsyncPayloadMachineFluentFsm _m;
            public ConcurrentFluentWrapper(Tests.Async.Features.Payload.ConcurrentAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault | ApiCapabilities.HasInternalTransitions;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // InitialOnEntry
        internal sealed class InitialOnEntryLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.InitialOnEntryAsyncPayloadMachine _m;
            public InitialOnEntryLegacy(Tests.Async.Features.Payload.InitialOnEntryAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class InitialOnEntryFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.InitialOnEntryAsyncPayloadMachineFluentFsm _m;
            public InitialOnEntryFluentWrapper(Tests.Async.Features.Payload.InitialOnEntryAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // MultiPayload
        internal sealed class MultiLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.MultiPayloadAsyncMachine _m;
            public MultiLegacy(Tests.Async.Features.Payload.MultiPayloadAsyncMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsMulti;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class MultiFluentWrapper : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Payload.MultiPayloadAsyncMachineFluentFsm _m;
            public MultiFluentWrapper(Tests.Async.Features.Payload.MultiPayloadAsyncMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsMulti;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
        }

        // PayloadMachine (from SimpleTokenTests)
        internal sealed class PMachineLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.PayloadMachine _m;
            public PMachineLegacy(Tests.Async.Features.Cancellation.PayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class PMachineFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.PayloadMachineFluentFsm _m;
            public PMachineFluent(Tests.Async.Features.Cancellation.PayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((Tests.Async.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
        }
    }

    internal static class CancellationWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        internal sealed class BasicLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.BasicTokenMachine _m;
            public BasicLegacy(Tests.Async.Features.Cancellation.BasicTokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class BasicFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.BasicTokenMachineFluentFsm _m;
            public BasicFluent(Tests.Async.Features.Cancellation.BasicTokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }

        // SpecificationComplianceMachine
        internal sealed class SpecLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.SpecificationComplianceMachine _m;
            public SpecLegacy(Tests.Async.Features.Cancellation.SpecificationComplianceMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger);
        }
        internal sealed class SpecFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.SpecificationComplianceMachineFluentFsm _m;
            public SpecFluent(Tests.Async.Features.Cancellation.SpecificationComplianceMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.SpecTriggers)trigger);
        }

        // SimpleCancellationMachine
        internal sealed class SimpleCancelLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.SimpleCancellationMachine _m;
            public SimpleCancelLegacy(Tests.Async.Features.Cancellation.SimpleCancellationMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger);
        }
        internal sealed class SimpleCancelFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.SimpleCancellationMachineFluentFsm _m;
            public SimpleCancelFluent(Tests.Async.Features.Cancellation.SimpleCancellationMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.SimpleTriggers)trigger);
        }

        // TokenMachine
        internal sealed class TokenLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.TokenMachine _m;
            public TokenLegacy(Tests.Async.Features.Cancellation.TokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger);
        }
        internal sealed class TokenFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.TokenMachineFluentFsm _m;
            public TokenFluent(Tests.Async.Features.Cancellation.TokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTriggers)trigger);
        }

        internal sealed class OptionalLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.OptionalTokenMachine _m;
            public OptionalLegacy(Tests.Async.Features.Cancellation.OptionalTokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class OptionalFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.OptionalTokenMachineFluentFsm _m;
            public OptionalFluent(Tests.Async.Features.Cancellation.OptionalTokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }

        internal sealed class CancellationLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.CancellationMachine _m;
            public CancellationLegacy(Tests.Async.Features.Cancellation.CancellationMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class CancellationFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.CancellationMachineFluentFsm _m;
            public CancellationFluent(Tests.Async.Features.Cancellation.CancellationMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }

        internal sealed class MixedLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.MixedTokenMachine _m;
            public MixedLegacy(Tests.Async.Features.Cancellation.MixedTokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class MixedFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Cancellation.MixedTokenMachineFluentFsm _m;
            public MixedFluent(Tests.Async.Features.Cancellation.MixedTokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Cancellation.TokenTestTrigger)trigger);
        }
    }

    internal static class ExceptionWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        private static ValueTask<bool> TryFireAsync<TState, TTrigger>(dynamic m, object trigger)
            => m.TryFireAsync((TTrigger)trigger);

        // Generic pattern per machine
        internal sealed class OnEntryContinueLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.OnEntryContinueMachine _m;
            public OnEntryContinueLegacy(Tests.Async.Features.Exceptions.OnEntryContinueMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class OnEntryContinueFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.OnEntryContinueMachineFluentFsm _m;
            public OnEntryContinueFluent(Tests.Async.Features.Exceptions.OnEntryContinueMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class ActionPropagateLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.ActionPropagateMachine _m;
            public ActionPropagateLegacy(Tests.Async.Features.Exceptions.ActionPropagateMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class ActionPropagateFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.ActionPropagateMachineFluentFsm _m;
            public ActionPropagateFluent(Tests.Async.Features.Exceptions.ActionPropagateMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class GuardExceptionLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.GuardExceptionMachine _m;
            public GuardExceptionLegacy(Tests.Async.Features.Exceptions.GuardExceptionMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class GuardExceptionFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.GuardExceptionMachineFluentFsm _m;
            public GuardExceptionFluent(Tests.Async.Features.Exceptions.GuardExceptionMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class CancellationPropagationLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.CancellationPropagationMachine _m;
            public CancellationPropagationLegacy(Tests.Async.Features.Exceptions.CancellationPropagationMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class CancellationPropagationFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.CancellationPropagationMachineFluentFsm _m;
            public CancellationPropagationFluent(Tests.Async.Features.Exceptions.CancellationPropagationMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class AsyncHandlerLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.AsyncHandlerMachine _m;
            public AsyncHandlerLegacy(Tests.Async.Features.Exceptions.AsyncHandlerMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class AsyncHandlerFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.AsyncHandlerMachineFluentFsm _m;
            public AsyncHandlerFluent(Tests.Async.Features.Exceptions.AsyncHandlerMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class ExceptionContextCaptureLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.ExceptionContextCaptureMachine _m;
            public ExceptionContextCaptureLegacy(Tests.Async.Features.Exceptions.ExceptionContextCaptureMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class ExceptionContextCaptureFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Exceptions.ExceptionContextCaptureMachineFluentFsm _m;
            public ExceptionContextCaptureFluent(Tests.Async.Features.Exceptions.ExceptionContextCaptureMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
    }

    internal static class ExtensionWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        internal sealed class SuccessLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Extensions.AsyncHookOrderMachineSuccess _m;
            public SuccessLegacy(Tests.Async.Features.Extensions.AsyncHookOrderMachineSuccess m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
        }
        internal sealed class SuccessFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Extensions.AsyncHookOrderMachineSuccessFluentFsm _m;
            public SuccessFluent(Tests.Async.Features.Extensions.AsyncHookOrderMachineSuccessFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
        }

        internal sealed class FailLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Extensions.AsyncHookOrderMachineFail _m;
            public FailLegacy(Tests.Async.Features.Extensions.AsyncHookOrderMachineFail m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
        }
        internal sealed class FailFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Extensions.AsyncHookOrderMachineFailFluentFsm _m;
            public FailFluent(Tests.Async.Features.Extensions.AsyncHookOrderMachineFailFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Extensions.ATrigger)trigger);
        }

        internal sealed class ExtLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Extensions.AsyncExtensionsMachine _m;
            public ExtLegacy(Tests.Async.Features.Extensions.AsyncExtensionsMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger);
        }
        internal sealed class ExtFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Extensions.AsyncExtensionsMachineFluentFsm _m;
            public ExtFluent(Tests.Async.Features.Extensions.AsyncExtensionsMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Extensions.ExtTrigger)trigger);
        }
    }

    internal static class ConcurrencyCoreWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        // RcMachine
        internal sealed class RcLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Concurrency.RcMachine _m;
            public RcLegacy(Tests.Async.Features.Concurrency.RcMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger);
        }
        internal sealed class RcFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Concurrency.RcMachineFluentFsm _m;
            public RcFluent(Tests.Async.Features.Concurrency.RcMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Concurrency.RcTriggers)trigger);
        }

        // SimpleAsyncMachine
        internal sealed class SimpleLegacy : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Core.SimpleAsyncMachine _m;
            public SimpleLegacy(Tests.Async.Features.Core.SimpleAsyncMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger);
        }
        internal sealed class SimpleFluent : IStateMachineTestWrapper
        {
            private readonly Tests.Async.Features.Core.SimpleAsyncMachineFluentFsm _m;
            public SimpleFluent(Tests.Async.Features.Core.SimpleAsyncMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Core.AsyncTriggers)trigger);
        }
    }

    // Cancellation factory methods
    public static partial class StateMachineWrapperFactory
    {
        private static IStateMachineTestWrapper CreateBasicToken(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("BasicToken", typeof(Tests.Async.Features.Cancellation.TokenTestState), initial);
            var s = (Tests.Async.Features.Cancellation.TokenTestState)Enum.Parse(typeof(Tests.Async.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.BasicLegacy(new Tests.Async.Features.Cancellation.BasicTokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.BasicFluent(new Tests.Async.Features.Cancellation.BasicTokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateOptionalToken(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("OptionalToken", typeof(Tests.Async.Features.Cancellation.TokenTestState), initial);
            var s = (Tests.Async.Features.Cancellation.TokenTestState)Enum.Parse(typeof(Tests.Async.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.OptionalLegacy(new Tests.Async.Features.Cancellation.OptionalTokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.OptionalFluent(new Tests.Async.Features.Cancellation.OptionalTokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateCancellation(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("Cancellation", typeof(Tests.Async.Features.Cancellation.TokenTestState), initial);
            var s = (Tests.Async.Features.Cancellation.TokenTestState)Enum.Parse(typeof(Tests.Async.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.CancellationLegacy(new Tests.Async.Features.Cancellation.CancellationMachine(s)),
                ApiType.Fluent => new CancellationWrappers.CancellationFluent(new Tests.Async.Features.Cancellation.CancellationMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateMixedToken(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("MixedToken", typeof(Tests.Async.Features.Cancellation.TokenTestState), initial);
            var s = (Tests.Async.Features.Cancellation.TokenTestState)Enum.Parse(typeof(Tests.Async.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.MixedLegacy(new Tests.Async.Features.Cancellation.MixedTokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.MixedFluent(new Tests.Async.Features.Cancellation.MixedTokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Exceptions factory methods
        private static IStateMachineTestWrapper CreateOnEntryContinue(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("OnEntryContinue", typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.OnEntryContinueLegacy(new Tests.Async.Features.Exceptions.OnEntryContinueMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.OnEntryContinueFluent(new Tests.Async.Features.Exceptions.OnEntryContinueMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateActionPropagate(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ActionPropagate", typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.ActionPropagateLegacy(new Tests.Async.Features.Exceptions.ActionPropagateMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.ActionPropagateFluent(new Tests.Async.Features.Exceptions.ActionPropagateMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateGuardException(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("GuardException", typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.GuardExceptionLegacy(new Tests.Async.Features.Exceptions.GuardExceptionMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.GuardExceptionFluent(new Tests.Async.Features.Exceptions.GuardExceptionMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateCancellationPropagation(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("CancellationPropagation", typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.CancellationPropagationLegacy(new Tests.Async.Features.Exceptions.CancellationPropagationMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.CancellationPropagationFluent(new Tests.Async.Features.Exceptions.CancellationPropagationMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateAsyncHandler(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("AsyncHandler", typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.AsyncHandlerLegacy(new Tests.Async.Features.Exceptions.AsyncHandlerMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.AsyncHandlerFluent(new Tests.Async.Features.Exceptions.AsyncHandlerMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExceptionContextCapture(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExceptionContextCapture", typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.ExceptionContextCaptureLegacy(new Tests.Async.Features.Exceptions.ExceptionContextCaptureMachine(s, _ => FastFsm.Exceptions.ExceptionDirective.Continue)),
                ApiType.Fluent => new ExceptionWrappers.ExceptionContextCaptureFluent(new Tests.Async.Features.Exceptions.ExceptionContextCaptureMachineFluentFsm(s, _ => FastFsm.Exceptions.ExceptionDirective.Continue)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Extensions factories
        private static IStateMachineTestWrapper CreateExtensionsSuccess(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExtensionsSuccess", typeof(Tests.Async.Features.Extensions.AState), initial);
            var s = (Tests.Async.Features.Extensions.AState)Enum.Parse(typeof(Tests.Async.Features.Extensions.AState), sName);
            return api switch
            {
                ApiType.Legacy => new ExtensionWrappers.SuccessLegacy(new Tests.Async.Features.Extensions.AsyncHookOrderMachineSuccess(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                ApiType.Fluent => new ExtensionWrappers.SuccessFluent(new Tests.Async.Features.Extensions.AsyncHookOrderMachineSuccessFluentFsm(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExtensionsFail(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExtensionsFail", typeof(Tests.Async.Features.Extensions.AState), initial);
            var s = (Tests.Async.Features.Extensions.AState)Enum.Parse(typeof(Tests.Async.Features.Extensions.AState), sName);
            return api switch
            {
                ApiType.Legacy => new ExtensionWrappers.FailLegacy(new Tests.Async.Features.Extensions.AsyncHookOrderMachineFail(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                ApiType.Fluent => new ExtensionWrappers.FailFluent(new Tests.Async.Features.Extensions.AsyncHookOrderMachineFailFluentFsm(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Concurrency/Core factories
        private static IStateMachineTestWrapper CreateRcMachine(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("RcMachine", typeof(Tests.Async.Features.Concurrency.RcStates), initial);
            var s = (Tests.Async.Features.Concurrency.RcStates)Enum.Parse(typeof(Tests.Async.Features.Concurrency.RcStates), sName);
            return api switch
            {
                ApiType.Legacy => new ConcurrencyCoreWrappers.RcLegacy(new Tests.Async.Features.Concurrency.RcMachine(s)),
                ApiType.Fluent => new ConcurrencyCoreWrappers.RcFluent(new Tests.Async.Features.Concurrency.RcMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateSimpleAsync(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("SimpleAsync", typeof(Tests.Async.Features.Core.AsyncStates), initial);
            var s = (Tests.Async.Features.Core.AsyncStates)Enum.Parse(typeof(Tests.Async.Features.Core.AsyncStates), sName);
            return api switch
            {
                ApiType.Legacy => new ConcurrencyCoreWrappers.SimpleLegacy(new Tests.Async.Features.Core.SimpleAsyncMachine(s)),
                ApiType.Fluent => new ConcurrencyCoreWrappers.SimpleFluent(new Tests.Async.Features.Core.SimpleAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateTinyAsyncHsm(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("TinyAsyncHsm", typeof(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), initial);
            var s = (Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S)Enum.Parse(typeof(Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), sName);
            return api switch
            {
                ApiType.Legacy => new HsmWrappers.TinyLegacy(new Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsm(s)),
                ApiType.Fluent => new HsmWrappers.TinyFluent(new Tests.Async.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsmFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateSpecificationCompliance(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("SpecificationComplianceMachine", typeof(Tests.Async.Features.Cancellation.SpecStates), initial);
            var s = (Tests.Async.Features.Cancellation.SpecStates)Enum.Parse(typeof(Tests.Async.Features.Cancellation.SpecStates), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.SpecLegacy(new Tests.Async.Features.Cancellation.SpecificationComplianceMachine(s)),
                ApiType.Fluent => new CancellationWrappers.SpecFluent(new Tests.Async.Features.Cancellation.SpecificationComplianceMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateSimpleCancellation(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("SimpleCancellationMachine", typeof(Tests.Async.Features.Cancellation.SimpleStates), initial);
            var s = (Tests.Async.Features.Cancellation.SimpleStates)Enum.Parse(typeof(Tests.Async.Features.Cancellation.SimpleStates), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.SimpleCancelLegacy(new Tests.Async.Features.Cancellation.SimpleCancellationMachine(s)),
                ApiType.Fluent => new CancellationWrappers.SimpleCancelFluent(new Tests.Async.Features.Cancellation.SimpleCancellationMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateTokenMachine(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("TokenMachine", typeof(Tests.Async.Features.Cancellation.TokenStates), initial);
            var s = (Tests.Async.Features.Cancellation.TokenStates)Enum.Parse(typeof(Tests.Async.Features.Cancellation.TokenStates), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.TokenLegacy(new Tests.Async.Features.Cancellation.TokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.TokenFluent(new Tests.Async.Features.Cancellation.TokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreatePayloadMachine(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("PayloadMachine", typeof(Tests.Async.Features.Cancellation.PayloadStates), initial);
            var s = (Tests.Async.Features.Cancellation.PayloadStates)Enum.Parse(typeof(Tests.Async.Features.Cancellation.PayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.PMachineLegacy(new Tests.Async.Features.Cancellation.PayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.PMachineFluent(new Tests.Async.Features.Cancellation.PayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateAsyncExtensions(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("AsyncExtensionsMachine", typeof(Tests.Async.Features.Extensions.ExtState), initial);
            var s = (Tests.Async.Features.Extensions.ExtState)Enum.Parse(typeof(Tests.Async.Features.Extensions.ExtState), sName);
            return api switch
            {
                ApiType.Legacy => new ExtensionWrappers.ExtLegacy(new Tests.Async.Features.Extensions.AsyncExtensionsMachine(s, new FastFsm.Contracts.IStateMachineExtension[]{})),
                ApiType.Fluent => new ExtensionWrappers.ExtFluent(new Tests.Async.Features.Extensions.AsyncExtensionsMachineFluentFsm(s, new FastFsm.Contracts.IStateMachineExtension[]{})),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateExceptionAsync(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExceptionAsyncMachine", typeof(Tests.Async.Features.Exceptions.ExStates), initial);
            var s = (Tests.Async.Features.Exceptions.ExStates)Enum.Parse(typeof(Tests.Async.Features.Exceptions.ExStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionAsyncLegacy(new Tests.Async.Features.Exceptions.ExceptionAsyncMachine(s)),
                ApiType.Fluent => new ExceptionAsyncFluent(new Tests.Async.Features.Exceptions.ExceptionAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    // Dedicated wrappers for ExceptionAsyncMachine
    internal sealed class ExceptionAsyncLegacy : IStateMachineTestWrapper
    {
        private readonly Tests.Async.Features.Exceptions.ExceptionAsyncMachine _m;
        public ExceptionAsyncLegacy(Tests.Async.Features.Exceptions.ExceptionAsyncMachine m) => _m = m;
        public object CurrentState => _m.CurrentState!;
        public ApiCapabilities Caps => ApiCapabilities.HasAsync;
        public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
        public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public IReadOnlyList<object> GetPermittedTriggers() => _m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult().Cast<object>().ToList();
        public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger);
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger);
    }
    internal sealed class ExceptionAsyncFluent : IStateMachineTestWrapper
    {
        private readonly Tests.Async.Features.Exceptions.ExceptionAsyncMachineFluentFsm _m;
        public ExceptionAsyncFluent(Tests.Async.Features.Exceptions.ExceptionAsyncMachineFluentFsm m) => _m = m;
        public object CurrentState => _m.CurrentState!;
        public ApiCapabilities Caps => ApiCapabilities.HasAsync;
        public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
        public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public void Fire(object trigger, object? payload = null) => _m.FireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public bool CanFire(object trigger) => _m.CanFireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public IReadOnlyList<object> GetPermittedTriggers() => _m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult().Cast<object>().ToList();
        public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger);
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((Tests.Async.Features.Exceptions.ExTriggers)trigger);
    }

    // Payload factory methods
    public static partial class StateMachineWrapperFactory
    {
        private static IStateMachineTestWrapper CreateBasicPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("BasicPayload", typeof(Tests.Async.Features.Payload.AsyncPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.BasicLegacy(new Tests.Async.Features.Payload.BasicAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.BasicFluentWrapper(new Tests.Async.Features.Payload.BasicAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateOverloadedPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("OverloadedPayload", typeof(Tests.Async.Features.Payload.AsyncPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.OverloadedLegacy(new Tests.Async.Features.Payload.OverloadedAsyncMachine(s)),
                ApiType.Fluent => new PayloadWrappers.OverloadedFluentWrapper(new Tests.Async.Features.Payload.OverloadedAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExceptionPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExceptionPayload", typeof(Tests.Async.Features.Payload.AsyncPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.ExceptionLegacy(new Tests.Async.Features.Payload.ExceptionAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.ExceptionFluentWrapper(new Tests.Async.Features.Payload.ExceptionAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateCanFirePayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("CanFirePayload", typeof(Tests.Async.Features.Payload.AsyncPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.CanFireLegacy(new Tests.Async.Features.Payload.CanFireAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.CanFireFluentWrapper(new Tests.Async.Features.Payload.CanFireAsyncPayloadMachineFluentFsm(s, threshold: 0)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateConcurrentPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ConcurrentPayload", typeof(Tests.Async.Features.Payload.AsyncPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.ConcurrentLegacy(new Tests.Async.Features.Payload.ConcurrentAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.ConcurrentFluentWrapper(new Tests.Async.Features.Payload.ConcurrentAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateInitialOnEntryPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("InitialOnEntryPayload", typeof(Tests.Async.Features.Payload.AsyncPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.InitialOnEntryLegacy(new Tests.Async.Features.Payload.InitialOnEntryAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.InitialOnEntryFluentWrapper(new Tests.Async.Features.Payload.InitialOnEntryAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateMultiPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("MultiPayload", typeof(Tests.Async.Features.Payload.MultiPayloadStates), initial);
            var s = (Tests.Async.Features.Payload.MultiPayloadStates)Enum.Parse(typeof(Tests.Async.Features.Payload.MultiPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.MultiLegacy(new Tests.Async.Features.Payload.MultiPayloadAsyncMachine(s)),
                ApiType.Fluent => new PayloadWrappers.MultiFluentWrapper(new Tests.Async.Features.Payload.MultiPayloadAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
