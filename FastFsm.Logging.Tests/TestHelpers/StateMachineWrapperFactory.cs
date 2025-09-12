using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastFsm.Logging.Tests.TestHelpers
{
    public static class StateMachineWrapperFactory
    {
        public enum ApiType { Fluent, Legacy }

        private static Type GetStateEnumType(string machine, ApiType api) =>
            MachineTypeRegistry.GetStateType(machine, api == ApiType.Fluent ? MachineTypeRegistry.Api.Fluent : MachineTypeRegistry.Api.Legacy);
        private static Type GetTriggerEnumType(string machine, ApiType api) =>
            MachineTypeRegistry.GetTriggerType(machine, api == ApiType.Fluent ? MachineTypeRegistry.Api.Fluent : MachineTypeRegistry.Api.Legacy);

        public static object GetStateEnum(string machine, ApiType api, string name) => Enum.Parse(GetStateEnumType(machine, api), name, ignoreCase: false);
        public static object GetTriggerEnum(string machine, ApiType api, string name) => Enum.Parse(GetTriggerEnumType(machine, api), name, ignoreCase: false);

        private static readonly Dictionary<string, Func<ApiType, string?, IStateMachineTestWrapper>> _factory = new(StringComparer.Ordinal)
        {
            ["PureStateMachine"] = CreatePure,
            ["BasicStateMachine"] = CreateBasic,
            ["PayloadStateMachine"] = CreatePayload,
            ["ExtensionsStateMachine"] = CreateExtensions,
            ["FullStateMachine"] = CreateFull,
            ["MultiPayloadStateMachine"] = CreateMultiPayload,
            ["HsmMachine"] = CreateHsm,
        };

        public static IStateMachineTestWrapper Create(string machineType, ApiType apiType, string? initialStateName)
        {
            if (!_factory.TryGetValue(machineType, out var f))
                throw new NotSupportedException($"Machine type '{machineType}' not supported");
            return f(apiType, initialStateName);
        }

        // Wrapper helpers
        private static IReadOnlyList<object> ToObjects<T>(IReadOnlyList<T> list) where T:struct,Enum => list.Cast<object>().ToList();
        private static ApiCapabilities Caps(bool async=false,bool defPayload=false,bool multi=false,bool internalT=false,bool hsm=false) =>
            (async?ApiCapabilities.HasAsync:0) | (defPayload?ApiCapabilities.HasDefaultPayload:0) | (multi?ApiCapabilities.HasMultiPayloads:0) | (internalT?ApiCapabilities.HasInternalTransitions:0) | (hsm?ApiCapabilities.IsHierarchical:0);

        // Machines.cs wrappers (Legacy)
        private class PureLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Logging.Tests.PureStateMachine _m;
            public PureLegacy(FastFsm.Logging.Tests.PureStateMachine m) => _m = m;
            public object CurrentState => _m.CurrentState!;
            public ApiCapabilities Caps => Caps();
            public void Start() => _m.Start();
            public bool TryFire(object trigger, object? payload = null) => _m.TryFire((FastFsm.Logging.Tests.TestTrigger)trigger);
            public void Fire(object trigger, object? payload = null) => _m.Fire((FastFsm.Logging.Tests.TestTrigger)trigger);
            public bool CanFire(object trigger) => _m.CanFire((FastFsm.Logging.Tests.TestTrigger)trigger);
            public IReadOnlyList<object> GetPermittedTriggers() => _m.GetPermittedTriggers().Cast<object>().ToList();
            public ValueTask StartAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => ValueTask.FromResult(TryFire(trigger));
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) { Fire(trigger); return ValueTask.CompletedTask; }
        }
        private sealed class BasicLegacy : PureLegacy { public BasicLegacy(FastFsm.Logging.Tests.BasicStateMachine m):base(m){} }
        private class PayloadLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Logging.Tests.PayloadStateMachine _m;
            public PayloadLegacy(FastFsm.Logging.Tests.PayloadStateMachine m)=>_m=m;
            public object CurrentState=>_m.CurrentState!;
            public ApiCapabilities Caps=>Caps(defPayload:true);
            public void Start()=>_m.Start();
            public bool TryFire(object trigger, object? payload=null)=> ((dynamic)_m).TryFire((FastFsm.Logging.Tests.TestTrigger)trigger,(dynamic?)payload);
            public void Fire(object trigger, object? payload=null)=> ((dynamic)_m).Fire((FastFsm.Logging.Tests.TestTrigger)trigger,(dynamic?)payload);
            public bool CanFire(object trigger)=> _m.CanFire((FastFsm.Logging.Tests.TestTrigger)trigger);
            public IReadOnlyList<object> GetPermittedTriggers()=> _m.GetPermittedTriggers().Cast<object>().ToList();
            public ValueTask StartAsync(CancellationToken ct=default)=> ValueTask.CompletedTask;
            public ValueTask<bool> TryFireAsync(object trigger, object? payload=null, CancellationToken ct=default)=> ValueTask.FromResult(TryFire(trigger,payload));
            public ValueTask FireAsync(object trigger, object? payload=null, CancellationToken ct=default){ Fire(trigger,payload); return ValueTask.CompletedTask; }
        }
        private sealed class ExtensionsLegacy : PureLegacy { public ExtensionsLegacy(FastFsm.Logging.Tests.ExtensionsStateMachine m):base(m){} }
        private sealed class FullLegacy : PayloadLegacy { public FullLegacy(FastFsm.Logging.Tests.FullStateMachine m):base(m){} }
        private sealed class MultiPayloadLegacy : PureLegacy { public MultiPayloadLegacy(FastFsm.Logging.Tests.MultiPayloadStateMachine m):base(m){} }

        // HSM Legacy
        private sealed class HsmLegacy : IStateMachineTestWrapper
        {
            private readonly FastFsm.Logging.Tests.HsmMachine _m;
            public HsmLegacy(FastFsm.Logging.Tests.HsmMachine m)=>_m=m;
            public object CurrentState=>_m.CurrentState!;
            public ApiCapabilities Caps=>Caps(hsm:true);
            public void Start()=> _m.StartAsync().AsTask().GetAwaiter().GetResult();
            public bool TryFire(object trigger, object? payload=null)=> _m.TryFireAsync((FastFsm.Logging.Tests.HTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public void Fire(object trigger, object? payload=null)=> _m.FireAsync((FastFsm.Logging.Tests.HTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public bool CanFire(object trigger)=> _m.CanFireAsync((FastFsm.Logging.Tests.HTrigger)trigger).AsTask().GetAwaiter().GetResult();
            public IReadOnlyList<object> GetPermittedTriggers()=> _m.GetPermittedTriggers().Cast<object>().ToList();
            public ValueTask StartAsync(CancellationToken ct=default)=> _m.StartAsync(ct);
            public ValueTask<bool> TryFireAsync(object trigger, object? payload=null, CancellationToken ct=default)=> _m.TryFireAsync((FastFsm.Logging.Tests.HTrigger)trigger, ct: ct);
            public ValueTask FireAsync(object trigger, object? payload=null, CancellationToken ct=default)=> _m.FireAsync((FastFsm.Logging.Tests.HTrigger)trigger, ct: ct);
        }

        // Fluent (na razie NotImplemented – dopełnimy po dodaniu FluentFsm maszyn)
        private sealed class NotImplementedFluent : IStateMachineTestWrapper
        {
            public object CurrentState => throw new NotImplementedException();
            public ApiCapabilities Caps => ApiCapabilities.None;
            public void Start() => throw new NotImplementedException();
            public bool TryFire(object trigger, object? payload = null) => throw new NotImplementedException();
            public void Fire(object trigger, object? payload = null) => throw new NotImplementedException();
            public bool CanFire(object trigger) => throw new NotImplementedException();
            public IReadOnlyList<object> GetPermittedTriggers() => throw new NotImplementedException();
            public ValueTask StartAsync(CancellationToken ct = default) => throw new NotImplementedException();
            public ValueTask<bool> TryFireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
            public ValueTask FireAsync(object trigger, object? payload = null, CancellationToken ct = default) => throw new NotImplementedException();
        }

        // Factory impls
        private static IStateMachineTestWrapper CreatePure(ApiType api, string? initial)
        {
            var sName = InitialStateResolver.Resolve("PureStateMachine", typeof(FastFsm.Logging.Tests.TestState), initial);
            var s = (FastFsm.Logging.Tests.TestState)Enum.Parse(typeof(FastFsm.Logging.Tests.TestState), sName);
            return api switch
            {
                ApiType.Legacy => new PureLegacy(new FastFsm.Logging.Tests.PureStateMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateBasic(ApiType api, string? initial)
        {
            var s = (FastFsm.Logging.Tests.TestState)Enum.Parse(typeof(FastFsm.Logging.Tests.TestState), InitialStateResolver.Resolve("BasicStateMachine", typeof(FastFsm.Logging.Tests.TestState), initial));
            return api switch
            {
                ApiType.Legacy => new BasicLegacy(new FastFsm.Logging.Tests.BasicStateMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreatePayload(ApiType api, string? initial)
        {
            var s = (FastFsm.Logging.Tests.TestState)Enum.Parse(typeof(FastFsm.Logging.Tests.TestState), InitialStateResolver.Resolve("PayloadStateMachine", typeof(FastFsm.Logging.Tests.TestState), initial));
            return api switch
            {
                ApiType.Legacy => new PayloadLegacy(new FastFsm.Logging.Tests.PayloadStateMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateExtensions(ApiType api, string? initial)
        {
            var s = (FastFsm.Logging.Tests.TestState)Enum.Parse(typeof(FastFsm.Logging.Tests.TestState), InitialStateResolver.Resolve("ExtensionsStateMachine", typeof(FastFsm.Logging.Tests.TestState), initial));
            return api switch
            {
                ApiType.Legacy => new ExtensionsLegacy(new FastFsm.Logging.Tests.ExtensionsStateMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateFull(ApiType api, string? initial)
        {
            var s = (FastFsm.Logging.Tests.TestState)Enum.Parse(typeof(FastFsm.Logging.Tests.TestState), InitialStateResolver.Resolve("FullStateMachine", typeof(FastFsm.Logging.Tests.TestState), initial));
            return api switch
            {
                ApiType.Legacy => new FullLegacy(new FastFsm.Logging.Tests.FullStateMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateMultiPayload(ApiType api, string? initial)
        {
            var s = (FastFsm.Logging.Tests.TestState)Enum.Parse(typeof(FastFsm.Logging.Tests.TestState), InitialStateResolver.Resolve("MultiPayloadStateMachine", typeof(FastFsm.Logging.Tests.TestState), initial));
            return api switch
            {
                ApiType.Legacy => new MultiPayloadLegacy(new FastFsm.Logging.Tests.MultiPayloadStateMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        private static IStateMachineTestWrapper CreateHsm(ApiType api, string? initial)
        {
            var s = (FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HState)Enum.Parse(typeof(FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HState), InitialStateResolver.Resolve("HsmMachine", typeof(FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HState), initial));
            return api switch
            {
                ApiType.Legacy => new HsmLegacy(new FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HsmMachine(s)),
                ApiType.Fluent => new NotImplementedFluent(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
