using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastFsm.Async.Tests.Features.Hsm.Runtime;

namespace FastFsm.Async.Tests.TestHelpers
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
            private readonly FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsm _m;
            public TinyLegacy(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
        }
        internal sealed class TinyFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsmFluentFsm _m;
            public TinyFluent(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsmFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => HsmCaps();
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.T)trigger);
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
            private readonly FastFsm.Async.Tests.Features.Payload.BasicAsyncPayloadMachine _m;
            public BasicLegacy(FastFsm.Async.Tests.Features.Payload.BasicAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class BasicFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.BasicAsyncPayloadMachineFluentFsm _m;
            public BasicFluentWrapper(FastFsm.Async.Tests.Features.Payload.BasicAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // Overloaded
        internal sealed class OverloadedLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.OverloadedAsyncMachine _m;
            public OverloadedLegacy(FastFsm.Async.Tests.Features.Payload.OverloadedAsyncMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class OverloadedFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.OverloadedAsyncMachineFluentFsm _m;
            public OverloadedFluentWrapper(FastFsm.Async.Tests.Features.Payload.OverloadedAsyncMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // Exception
        internal sealed class ExceptionLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.ExceptionAsyncPayloadMachine _m;
            public ExceptionLegacy(FastFsm.Async.Tests.Features.Payload.ExceptionAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class ExceptionFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.ExceptionAsyncPayloadMachineFluentFsm _m;
            public ExceptionFluentWrapper(FastFsm.Async.Tests.Features.Payload.ExceptionAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // CanFire
        internal sealed class CanFireLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.CanFireAsyncPayloadMachine _m;
            public CanFireLegacy(FastFsm.Async.Tests.Features.Payload.CanFireAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 1 }).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class CanFireFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.CanFireAsyncPayloadMachineFluentFsm _m;
            public CanFireFluentWrapper(FastFsm.Async.Tests.Features.Payload.CanFireAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => ((dynamic)_m).CanFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 1 }).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // Concurrent
        internal sealed class ConcurrentLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.ConcurrentAsyncPayloadMachine _m;
            public ConcurrentLegacy(FastFsm.Async.Tests.Features.Payload.ConcurrentAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault | ApiCapabilities.HasInternalTransitions;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class ConcurrentFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.ConcurrentAsyncPayloadMachineFluentFsm _m;
            public ConcurrentFluentWrapper(FastFsm.Async.Tests.Features.Payload.ConcurrentAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault | ApiCapabilities.HasInternalTransitions;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // InitialOnEntry
        internal sealed class InitialOnEntryLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.InitialOnEntryAsyncPayloadMachine _m;
            public InitialOnEntryLegacy(FastFsm.Async.Tests.Features.Payload.InitialOnEntryAsyncPayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class InitialOnEntryFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.InitialOnEntryAsyncPayloadMachineFluentFsm _m;
            public InitialOnEntryFluentWrapper(FastFsm.Async.Tests.Features.Payload.InitialOnEntryAsyncPayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.AsyncPayloadTriggers)trigger, (dynamic?)payload);
        }

        // MultiPayload
        internal sealed class MultiLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.MultiPayloadAsyncMachine _m;
            public MultiLegacy(FastFsm.Async.Tests.Features.Payload.MultiPayloadAsyncMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsMulti;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class MultiFluentWrapper : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Payload.MultiPayloadAsyncMachineFluentFsm _m;
            public MultiFluentWrapper(FastFsm.Async.Tests.Features.Payload.MultiPayloadAsyncMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsMulti;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => true;
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(((dynamic)_m).GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Payload.MultiPayloadTriggers)trigger, (dynamic?)payload);
        }

        // PayloadMachine (from SimpleTokenTests)
        internal sealed class PMachineLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.PayloadMachine _m;
            public PMachineLegacy(FastFsm.Async.Tests.Features.Cancellation.PayloadMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
        }
        internal sealed class PMachineFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.PayloadMachineFluentFsm _m;
            public PMachineFluent(FastFsm.Async.Tests.Features.Cancellation.PayloadMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CapsDefault;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ((dynamic)_m).FireAsync((FastFsm.Async.Tests.Features.Cancellation.PayloadTriggers)trigger, (dynamic?)payload);
        }
    }

    internal static class CancellationWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        internal sealed class BasicLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.BasicTokenMachine _m;
            public BasicLegacy(FastFsm.Async.Tests.Features.Cancellation.BasicTokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class BasicFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.BasicTokenMachineFluentFsm _m;
            public BasicFluent(FastFsm.Async.Tests.Features.Cancellation.BasicTokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }

        // SpecificationComplianceMachine
        internal sealed class SpecLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.SpecificationComplianceMachine _m;
            public SpecLegacy(FastFsm.Async.Tests.Features.Cancellation.SpecificationComplianceMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger);
        }
        internal sealed class SpecFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.SpecificationComplianceMachineFluentFsm _m;
            public SpecFluent(FastFsm.Async.Tests.Features.Cancellation.SpecificationComplianceMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SpecTriggers)trigger);
        }

        // SimpleCancellationMachine
        internal sealed class SimpleCancelLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.SimpleCancellationMachine _m;
            public SimpleCancelLegacy(FastFsm.Async.Tests.Features.Cancellation.SimpleCancellationMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger);
        }
        internal sealed class SimpleCancelFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.SimpleCancellationMachineFluentFsm _m;
            public SimpleCancelFluent(FastFsm.Async.Tests.Features.Cancellation.SimpleCancellationMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.SimpleTriggers)trigger);
        }

        // TokenMachine
        internal sealed class TokenLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.TokenMachine _m;
            public TokenLegacy(FastFsm.Async.Tests.Features.Cancellation.TokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger);
        }
        internal sealed class TokenFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.TokenMachineFluentFsm _m;
            public TokenFluent(FastFsm.Async.Tests.Features.Cancellation.TokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTriggers)trigger);
        }

        internal sealed class OptionalLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.OptionalTokenMachine _m;
            public OptionalLegacy(FastFsm.Async.Tests.Features.Cancellation.OptionalTokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class OptionalFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.OptionalTokenMachineFluentFsm _m;
            public OptionalFluent(FastFsm.Async.Tests.Features.Cancellation.OptionalTokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }

        internal sealed class CancellationLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.CancellationMachine _m;
            public CancellationLegacy(FastFsm.Async.Tests.Features.Cancellation.CancellationMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class CancellationFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.CancellationMachineFluentFsm _m;
            public CancellationFluent(FastFsm.Async.Tests.Features.Cancellation.CancellationMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }

        internal sealed class MixedLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.MixedTokenMachine _m;
            public MixedLegacy(FastFsm.Async.Tests.Features.Cancellation.MixedTokenMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
        }
        internal sealed class MixedFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Cancellation.MixedTokenMachineFluentFsm _m;
            public MixedFluent(FastFsm.Async.Tests.Features.Cancellation.MixedTokenMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => CancellationWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Cancellation.TokenTestTrigger)trigger);
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
            private readonly FastFsm.Async.Tests.Features.Exceptions.OnEntryContinueMachine _m;
            public OnEntryContinueLegacy(FastFsm.Async.Tests.Features.Exceptions.OnEntryContinueMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class OnEntryContinueFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.OnEntryContinueMachineFluentFsm _m;
            public OnEntryContinueFluent(FastFsm.Async.Tests.Features.Exceptions.OnEntryContinueMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class ActionPropagateLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.ActionPropagateMachine _m;
            public ActionPropagateLegacy(FastFsm.Async.Tests.Features.Exceptions.ActionPropagateMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class ActionPropagateFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.ActionPropagateMachineFluentFsm _m;
            public ActionPropagateFluent(FastFsm.Async.Tests.Features.Exceptions.ActionPropagateMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class GuardExceptionLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.GuardExceptionMachine _m;
            public GuardExceptionLegacy(FastFsm.Async.Tests.Features.Exceptions.GuardExceptionMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class GuardExceptionFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.GuardExceptionMachineFluentFsm _m;
            public GuardExceptionFluent(FastFsm.Async.Tests.Features.Exceptions.GuardExceptionMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class CancellationPropagationLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.CancellationPropagationMachine _m;
            public CancellationPropagationLegacy(FastFsm.Async.Tests.Features.Exceptions.CancellationPropagationMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class CancellationPropagationFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.CancellationPropagationMachineFluentFsm _m;
            public CancellationPropagationFluent(FastFsm.Async.Tests.Features.Exceptions.CancellationPropagationMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class AsyncHandlerLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.AsyncHandlerMachine _m;
            public AsyncHandlerLegacy(FastFsm.Async.Tests.Features.Exceptions.AsyncHandlerMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class AsyncHandlerFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.AsyncHandlerMachineFluentFsm _m;
            public AsyncHandlerFluent(FastFsm.Async.Tests.Features.Exceptions.AsyncHandlerMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }

        internal sealed class ExceptionContextCaptureLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.ExceptionContextCaptureMachine _m;
            public ExceptionContextCaptureLegacy(FastFsm.Async.Tests.Features.Exceptions.ExceptionContextCaptureMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
        internal sealed class ExceptionContextCaptureFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Exceptions.ExceptionContextCaptureMachineFluentFsm _m;
            public ExceptionContextCaptureFluent(FastFsm.Async.Tests.Features.Exceptions.ExceptionContextCaptureMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExceptionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExceptionTestTriggers)trigger);
        }
    }

    internal static class ExtensionWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        internal sealed class SuccessLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineSuccess _m;
            public SuccessLegacy(FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineSuccess m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
        }
        internal sealed class SuccessFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineSuccessFluentFsm _m;
            public SuccessFluent(FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineSuccessFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
        }

        internal sealed class FailLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineFail _m;
            public FailLegacy(FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineFail m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
        }
        internal sealed class FailFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineFailFluentFsm _m;
            public FailFluent(FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineFailFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ATrigger)trigger);
        }

        internal sealed class ExtLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Extensions.AsyncExtensionsMachine _m;
            public ExtLegacy(FastFsm.Async.Tests.Features.Extensions.AsyncExtensionsMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger);
        }
        internal sealed class ExtFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Extensions.AsyncExtensionsMachineFluentFsm _m;
            public ExtFluent(FastFsm.Async.Tests.Features.Extensions.AsyncExtensionsMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ExtensionWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Extensions.ExtTrigger)trigger);
        }
    }

    internal static class ConcurrencyCoreWrappers
    {
        private static IReadOnlyList<object> ToObjectList<T>(IReadOnlyList<T> items) where T : struct, Enum => items.Cast<object>().ToList();
        private static ApiCapabilities Caps => ApiCapabilities.HasAsync;

        // RcMachine
        internal sealed class RcLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Concurrency.RcMachine _m;
            public RcLegacy(FastFsm.Async.Tests.Features.Concurrency.RcMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger);
        }
        internal sealed class RcFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Concurrency.RcMachineFluentFsm _m;
            public RcFluent(FastFsm.Async.Tests.Features.Concurrency.RcMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Concurrency.RcTriggers)trigger);
        }

        // SimpleAsyncMachine
        internal sealed class SimpleLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Core.SimpleAsyncMachine _m;
            public SimpleLegacy(FastFsm.Async.Tests.Features.Core.SimpleAsyncMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger);
        }
        internal sealed class SimpleFluent : IStateMachineTestWrapper
        {
            private readonly FastFsm.Async.Tests.Features.Core.SimpleAsyncMachineFluentFsm _m;
            public SimpleFluent(FastFsm.Async.Tests.Features.Core.SimpleAsyncMachineFluentFsm m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => ConcurrencyCoreWrappers.Caps;
            public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers() => ToObjectList(_m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult());
            public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger);
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Core.AsyncTriggers)trigger);
        }
    }

    // Cancellation factory methods
    public static partial class StateMachineWrapperFactory
    {
        private static IStateMachineTestWrapper CreateBasicToken(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("BasicToken", typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.TokenTestState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.BasicLegacy(new FastFsm.Async.Tests.Features.Cancellation.BasicTokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.BasicFluent(new FastFsm.Async.Tests.Features.Cancellation.BasicTokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateOptionalToken(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("OptionalToken", typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.TokenTestState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.OptionalLegacy(new FastFsm.Async.Tests.Features.Cancellation.OptionalTokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.OptionalFluent(new FastFsm.Async.Tests.Features.Cancellation.OptionalTokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateCancellation(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("Cancellation", typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.TokenTestState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.CancellationLegacy(new FastFsm.Async.Tests.Features.Cancellation.CancellationMachine(s)),
                ApiType.Fluent => new CancellationWrappers.CancellationFluent(new FastFsm.Async.Tests.Features.Cancellation.CancellationMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateMixedToken(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("MixedToken", typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.TokenTestState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenTestState), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.MixedLegacy(new FastFsm.Async.Tests.Features.Cancellation.MixedTokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.MixedFluent(new FastFsm.Async.Tests.Features.Cancellation.MixedTokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Exceptions factory methods
        private static IStateMachineTestWrapper CreateOnEntryContinue(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("OnEntryContinue", typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.OnEntryContinueLegacy(new FastFsm.Async.Tests.Features.Exceptions.OnEntryContinueMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.OnEntryContinueFluent(new FastFsm.Async.Tests.Features.Exceptions.OnEntryContinueMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateActionPropagate(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ActionPropagate", typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.ActionPropagateLegacy(new FastFsm.Async.Tests.Features.Exceptions.ActionPropagateMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.ActionPropagateFluent(new FastFsm.Async.Tests.Features.Exceptions.ActionPropagateMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateGuardException(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("GuardException", typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.GuardExceptionLegacy(new FastFsm.Async.Tests.Features.Exceptions.GuardExceptionMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.GuardExceptionFluent(new FastFsm.Async.Tests.Features.Exceptions.GuardExceptionMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateCancellationPropagation(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("CancellationPropagation", typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.CancellationPropagationLegacy(new FastFsm.Async.Tests.Features.Exceptions.CancellationPropagationMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.CancellationPropagationFluent(new FastFsm.Async.Tests.Features.Exceptions.CancellationPropagationMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateAsyncHandler(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("AsyncHandler", typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.AsyncHandlerLegacy(new FastFsm.Async.Tests.Features.Exceptions.AsyncHandlerMachine(s)),
                ApiType.Fluent => new ExceptionWrappers.AsyncHandlerFluent(new FastFsm.Async.Tests.Features.Exceptions.AsyncHandlerMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExceptionContextCapture(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExceptionContextCapture", typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExceptionTestStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionWrappers.ExceptionContextCaptureLegacy(new FastFsm.Async.Tests.Features.Exceptions.ExceptionContextCaptureMachine(s, _ => FastFsm.Exceptions.ExceptionDirective.Continue)),
                ApiType.Fluent => new ExceptionWrappers.ExceptionContextCaptureFluent(new FastFsm.Async.Tests.Features.Exceptions.ExceptionContextCaptureMachineFluentFsm(s, _ => FastFsm.Exceptions.ExceptionDirective.Continue)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Extensions factories
        private static IStateMachineTestWrapper CreateExtensionsSuccess(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExtensionsSuccess", typeof(FastFsm.Async.Tests.Features.Extensions.AState), initial);
            var s = (FastFsm.Async.Tests.Features.Extensions.AState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Extensions.AState), sName);
            return api switch
            {
                ApiType.Legacy => new ExtensionWrappers.SuccessLegacy(new FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineSuccess(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                ApiType.Fluent => new ExtensionWrappers.SuccessFluent(new FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineSuccessFluentFsm(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExtensionsFail(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExtensionsFail", typeof(FastFsm.Async.Tests.Features.Extensions.AState), initial);
            var s = (FastFsm.Async.Tests.Features.Extensions.AState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Extensions.AState), sName);
            return api switch
            {
                ApiType.Legacy => new ExtensionWrappers.FailLegacy(new FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineFail(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                ApiType.Fluent => new ExtensionWrappers.FailFluent(new FastFsm.Async.Tests.Features.Extensions.AsyncHookOrderMachineFailFluentFsm(s, new FastFsm.Contracts.IStateMachineExtension[] { })),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        // Concurrency/Core factories
        private static IStateMachineTestWrapper CreateRcMachine(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("RcMachine", typeof(FastFsm.Async.Tests.Features.Concurrency.RcStates), initial);
            var s = (FastFsm.Async.Tests.Features.Concurrency.RcStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Concurrency.RcStates), sName);
            return api switch
            {
                ApiType.Legacy => new ConcurrencyCoreWrappers.RcLegacy(new FastFsm.Async.Tests.Features.Concurrency.RcMachine(s)),
                ApiType.Fluent => new ConcurrencyCoreWrappers.RcFluent(new FastFsm.Async.Tests.Features.Concurrency.RcMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateSimpleAsync(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("SimpleAsync", typeof(FastFsm.Async.Tests.Features.Core.AsyncStates), initial);
            var s = (FastFsm.Async.Tests.Features.Core.AsyncStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Core.AsyncStates), sName);
            return api switch
            {
                ApiType.Legacy => new ConcurrencyCoreWrappers.SimpleLegacy(new FastFsm.Async.Tests.Features.Core.SimpleAsyncMachine(s)),
                ApiType.Fluent => new ConcurrencyCoreWrappers.SimpleFluent(new FastFsm.Async.Tests.Features.Core.SimpleAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateTinyAsyncHsm(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("TinyAsyncHsm", typeof(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), initial);
            var s = (FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.S), sName);
            return api switch
            {
                ApiType.Legacy => new HsmWrappers.TinyLegacy(new FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsm(s)),
                ApiType.Fluent => new HsmWrappers.TinyFluent(new FastFsm.Async.Tests.Features.Hsm.CompileTime.AsyncNoActionHsmTests.TinyAsyncHsmFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateSpecificationCompliance(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("SpecificationComplianceMachine", typeof(FastFsm.Async.Tests.Features.Cancellation.SpecStates), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.SpecStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.SpecStates), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.SpecLegacy(new FastFsm.Async.Tests.Features.Cancellation.SpecificationComplianceMachine(s)),
                ApiType.Fluent => new CancellationWrappers.SpecFluent(new FastFsm.Async.Tests.Features.Cancellation.SpecificationComplianceMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateSimpleCancellation(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("SimpleCancellationMachine", typeof(FastFsm.Async.Tests.Features.Cancellation.SimpleStates), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.SimpleStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.SimpleStates), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.SimpleCancelLegacy(new FastFsm.Async.Tests.Features.Cancellation.SimpleCancellationMachine(s)),
                ApiType.Fluent => new CancellationWrappers.SimpleCancelFluent(new FastFsm.Async.Tests.Features.Cancellation.SimpleCancellationMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateTokenMachine(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("TokenMachine", typeof(FastFsm.Async.Tests.Features.Cancellation.TokenStates), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.TokenStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.TokenStates), sName);
            return api switch
            {
                ApiType.Legacy => new CancellationWrappers.TokenLegacy(new FastFsm.Async.Tests.Features.Cancellation.TokenMachine(s)),
                ApiType.Fluent => new CancellationWrappers.TokenFluent(new FastFsm.Async.Tests.Features.Cancellation.TokenMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreatePayloadMachine(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("PayloadMachine", typeof(FastFsm.Async.Tests.Features.Cancellation.PayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Cancellation.PayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Cancellation.PayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.PMachineLegacy(new FastFsm.Async.Tests.Features.Cancellation.PayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.PMachineFluent(new FastFsm.Async.Tests.Features.Cancellation.PayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateAsyncExtensions(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("AsyncExtensionsMachine", typeof(FastFsm.Async.Tests.Features.Extensions.ExtState), initial);
            var s = (FastFsm.Async.Tests.Features.Extensions.ExtState)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Extensions.ExtState), sName);
            return api switch
            {
                ApiType.Legacy => new ExtensionWrappers.ExtLegacy(new FastFsm.Async.Tests.Features.Extensions.AsyncExtensionsMachine(s, new FastFsm.Contracts.IStateMachineExtension[]{})),
                ApiType.Fluent => new ExtensionWrappers.ExtFluent(new FastFsm.Async.Tests.Features.Extensions.AsyncExtensionsMachineFluentFsm(s, new FastFsm.Contracts.IStateMachineExtension[]{})),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static IStateMachineTestWrapper CreateExceptionAsync(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExceptionAsyncMachine", typeof(FastFsm.Async.Tests.Features.Exceptions.ExStates), initial);
            var s = (FastFsm.Async.Tests.Features.Exceptions.ExStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Exceptions.ExStates), sName);
            return api switch
            {
                ApiType.Legacy => new ExceptionAsyncLegacy(new FastFsm.Async.Tests.Features.Exceptions.ExceptionAsyncMachine(s)),
                ApiType.Fluent => new ExceptionAsyncFluent(new FastFsm.Async.Tests.Features.Exceptions.ExceptionAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    // Dedicated wrappers for ExceptionAsyncMachine
    internal sealed class ExceptionAsyncLegacy : IStateMachineTestWrapper
    {
        private readonly FastFsm.Async.Tests.Features.Exceptions.ExceptionAsyncMachine _m;
        public ExceptionAsyncLegacy(FastFsm.Async.Tests.Features.Exceptions.ExceptionAsyncMachine m) => _m = m;
        public object CurrentState => _m.CurrentState!;
        public ApiCapabilities Caps => ApiCapabilities.HasAsync;
        public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
        public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public IReadOnlyList<object> GetPermittedTriggers() => _m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult().Cast<object>().ToList();
        public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger);
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger);
    }
    internal sealed class ExceptionAsyncFluent : IStateMachineTestWrapper
    {
        private readonly FastFsm.Async.Tests.Features.Exceptions.ExceptionAsyncMachineFluentFsm _m;
        public ExceptionAsyncFluent(FastFsm.Async.Tests.Features.Exceptions.ExceptionAsyncMachineFluentFsm m) => _m = m;
        public object CurrentState => _m.CurrentState!;
        public ApiCapabilities Caps => ApiCapabilities.HasAsync;
        public void Start() => _m.StartAsync().AsTask().GetAwaiter().GetResult();
        public bool TryFire(object trigger, object? payload = null) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public void Fire(object trigger, object? payload = null) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public bool CanFire(object trigger) => _m.CanFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger).AsTask().GetAwaiter().GetResult();
        public IReadOnlyList<object> GetPermittedTriggers() => _m.GetPermittedTriggersAsync().AsTask().GetAwaiter().GetResult().Cast<object>().ToList();
        public ValueTask StartAsync(CancellationToken ct = default) => _m.StartAsync(ct);
        public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.TryFireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger);
        public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => _m.FireAsync((FastFsm.Async.Tests.Features.Exceptions.ExTriggers)trigger);
    }

    // Payload factory methods
    public static partial class StateMachineWrapperFactory
    {
        private static IStateMachineTestWrapper CreateBasicPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("BasicPayload", typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.BasicLegacy(new FastFsm.Async.Tests.Features.Payload.BasicAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.BasicFluentWrapper(new FastFsm.Async.Tests.Features.Payload.BasicAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateOverloadedPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("OverloadedPayload", typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.OverloadedLegacy(new FastFsm.Async.Tests.Features.Payload.OverloadedAsyncMachine(s)),
                ApiType.Fluent => new PayloadWrappers.OverloadedFluentWrapper(new FastFsm.Async.Tests.Features.Payload.OverloadedAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExceptionPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ExceptionPayload", typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.ExceptionLegacy(new FastFsm.Async.Tests.Features.Payload.ExceptionAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.ExceptionFluentWrapper(new FastFsm.Async.Tests.Features.Payload.ExceptionAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateCanFirePayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("CanFirePayload", typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.CanFireLegacy(new FastFsm.Async.Tests.Features.Payload.CanFireAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.CanFireFluentWrapper(new FastFsm.Async.Tests.Features.Payload.CanFireAsyncPayloadMachineFluentFsm(s, threshold: 0)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateConcurrentPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("ConcurrentPayload", typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.ConcurrentLegacy(new FastFsm.Async.Tests.Features.Payload.ConcurrentAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.ConcurrentFluentWrapper(new FastFsm.Async.Tests.Features.Payload.ConcurrentAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateInitialOnEntryPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("InitialOnEntryPayload", typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.AsyncPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.InitialOnEntryLegacy(new FastFsm.Async.Tests.Features.Payload.InitialOnEntryAsyncPayloadMachine(s)),
                ApiType.Fluent => new PayloadWrappers.InitialOnEntryFluentWrapper(new FastFsm.Async.Tests.Features.Payload.InitialOnEntryAsyncPayloadMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateMultiPayload(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("MultiPayload", typeof(FastFsm.Async.Tests.Features.Payload.MultiPayloadStates), initial);
            var s = (FastFsm.Async.Tests.Features.Payload.MultiPayloadStates)Enum.Parse(typeof(FastFsm.Async.Tests.Features.Payload.MultiPayloadStates), sName);
            return api switch
            {
                ApiType.Legacy => new PayloadWrappers.MultiLegacy(new FastFsm.Async.Tests.Features.Payload.MultiPayloadAsyncMachine(s)),
                ApiType.Fluent => new PayloadWrappers.MultiFluentWrapper(new FastFsm.Async.Tests.Features.Payload.MultiPayloadAsyncMachineFluentFsm(s)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
