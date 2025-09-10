using System;
using System.Collections.Generic;
using System.Linq;
using FastFsm.Tests;
using FastFsm.Tests.Features.Core;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Central registry of all state machines and their type mappings
    /// </summary>
    public static class MachineRegistry
    {
        public class MachineInfo
        {
            public string Name { get; set; } = "";
            public Type? FluentStateType { get; set; }
            public Type? LegacyStateType { get; set; }
            public Type? FluentTriggerType { get; set; }
            public Type? LegacyTriggerType { get; set; }
            public Func<StateMachineWrapperFactory.ApiType, string, IStateMachineTestWrapper>? WrapperFactory { get; set; }
            public bool IsComplete => FluentStateType != null && LegacyStateType != null && 
                                     FluentTriggerType != null && LegacyTriggerType != null;
        }

        private static readonly Dictionary<string, MachineInfo> _machines = new();

        static MachineRegistry()
        {
            // Core machines from Features/Performance
            Register("CoreBenchmark", 
                typeof(BenchmarkTests.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkState),
                typeof(BenchmarkTests.BenchmarkTrigger), typeof(BenchmarkTestsLegacy.BenchmarkTrigger),
                (api, state) => api == StateMachineWrapperFactory.ApiType.Fluent 
                    ? new CoreBenchmarkFluentWrapper(state) 
                    : new CoreBenchmarkLegacyWrapper(state));

            // GuardPermitted from Features/Core
            Register("GuardPermitted",
                typeof(Features.Core.State), typeof(Features.Core.State),  // Same enum for both
                typeof(Features.Core.Trigger), typeof(Features.Core.Trigger),  // Same enum for both
                (api, state) => api == StateMachineWrapperFactory.ApiType.Fluent
                    ? new GuardPermittedFluentWrapper(state)
                    : new GuardPermittedLegacyWrapper(state));

            // Machines from Machines/ folder and test files
            
            Register("BasicBenchmark",
                typeof(BenchmarkTests.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkState),
                typeof(BenchmarkTests.BenchmarkTrigger), typeof(BenchmarkTestsLegacy.BenchmarkTrigger),
                null); // TODO: Create wrapper

            Register("CallbackOrder",
                typeof(Features.Core.StateCallbackTests.CallbackState), 
                typeof(Features.Core.StateCallbackTests.CallbackState),
                typeof(Features.Core.StateCallbackTests.CallbackTrigger), 
                typeof(Features.Core.StateCallbackTests.CallbackTrigger),
                null); // TODO: Create wrapper

            Register("CaseSensitive",
                typeof(Features.EdgeCases.NameCollisionTests.CaseSensitiveState), 
                typeof(Features.EdgeCases.NameCollisionTests.CaseSensitiveState),
                typeof(Features.EdgeCases.NameCollisionTests.CaseSensitiveTrigger), 
                typeof(Features.EdgeCases.NameCollisionTests.CaseSensitiveTrigger),
                null); // TODO: Create wrapper

            Register("ComplexCallback",
                typeof(Features.Core.StateCallbackTests.ComplexCallbackState), 
                typeof(Features.Core.StateCallbackTests.ComplexCallbackState),
                typeof(Features.Core.StateCallbackTests.ComplexCallbackTrigger), 
                typeof(Features.Core.StateCallbackTests.ComplexCallbackTrigger),
                null); // TODO: Create wrapper

            Register("ConflictingNames",
                typeof(Features.EdgeCases.NameCollisionTests.ConflictState), 
                typeof(Features.EdgeCases.NameCollisionTests.ConflictState),
                typeof(Features.EdgeCases.NameCollisionTests.ConflictTrigger), 
                typeof(Features.EdgeCases.NameCollisionTests.ConflictTrigger),
                null); // TODO: Create wrapper

            Register("ExceptionCallback",
                typeof(Features.Core.StateCallbackTests.ExceptionState), 
                typeof(Features.Core.StateCallbackTests.ExceptionState),
                typeof(Features.Core.StateCallbackTests.ExceptionTrigger), 
                typeof(Features.Core.StateCallbackTests.ExceptionTrigger),
                null); // TODO: Create wrapper

            Register("FullMultiPayload",
                typeof(Features.Payload.MultiState), 
                typeof(Features.Payload.MultiState),
                typeof(Features.Payload.MultiTrigger), 
                typeof(Features.Payload.MultiTrigger),
                (api, state) => api == StateMachineWrapperFactory.ApiType.Fluent
                    ? new MultiPayloadMachineFluentWrapper(state)
                    : new MultiPayloadMachineLegacyWrapper(state));

            Register("FullOrder",
                typeof(OrderState), 
                typeof(OrderState),
                typeof(OrderTrigger), 
                typeof(OrderTrigger),
                null); // TODO: Create wrapper

            Register("GuardedCallback",
                typeof(Features.Core.StateCallbackTests.GuardedState), 
                typeof(Features.Core.StateCallbackTests.GuardedState),
                typeof(Features.Core.StateCallbackTests.GuardedTrigger), 
                typeof(Features.Core.StateCallbackTests.GuardedTrigger),
                null); // TODO: Create wrapper

            Register("InitialState",
                typeof(Features.Core.StateCallbackTests.InitialState), 
                typeof(Features.Core.StateCallbackTests.InitialState),
                typeof(Features.Core.StateCallbackTests.InitialTrigger), 
                typeof(Features.Core.StateCallbackTests.InitialTrigger),
                null); // TODO: Create wrapper

            Register("InternalOnly",
                typeof(Features.EdgeCases.EmptyMachineTests.InternalOnlyState), 
                typeof(Features.EdgeCases.EmptyMachineTests.InternalOnlyState),
                typeof(Features.EdgeCases.EmptyMachineTests.InternalOnlyTrigger), 
                typeof(Features.EdgeCases.EmptyMachineTests.InternalOnlyTrigger),
                null); // TODO: Create wrapper

            Register("InternalTransition",
                typeof(Features.Core.StateCallbackTests.InternalState), 
                typeof(Features.Core.StateCallbackTests.InternalState),
                typeof(Features.Core.StateCallbackTests.InternalTrigger), 
                typeof(Features.Core.StateCallbackTests.InternalTrigger),
                null); // TODO: Create wrapper

            Register("KeywordState",
                typeof(Features.EdgeCases.NameCollisionTests.KeywordState), 
                typeof(Features.EdgeCases.NameCollisionTests.KeywordState),
                typeof(Features.EdgeCases.NameCollisionTests.KeywordTrigger), 
                typeof(Features.EdgeCases.NameCollisionTests.KeywordTrigger),
                null); // TODO: Create wrapper

            Register("LongName",
                typeof(Features.EdgeCases.NameCollisionTests.LongNameState), 
                typeof(Features.EdgeCases.NameCollisionTests.LongNameState),
                typeof(Features.EdgeCases.NameCollisionTests.LongNameTrigger), 
                typeof(Features.EdgeCases.NameCollisionTests.LongNameTrigger),
                null); // TODO: Create wrapper

            Register("MultipleCallbacks",
                typeof(Features.Core.StateCallbackTests.MultiState), 
                typeof(Features.Core.StateCallbackTests.MultiState),
                typeof(Features.Core.StateCallbackTests.MultiTrigger), 
                typeof(Features.Core.StateCallbackTests.MultiTrigger),
                null); // TODO: Create wrapper

            Register("NoGuardBenchmark",
                typeof(BenchmarkTests.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkState),
                typeof(BenchmarkTests.BenchmarkTrigger), typeof(BenchmarkTestsLegacy.BenchmarkTrigger),
                null); // TODO: Create wrapper

            Register("Numeric",
                typeof(Features.EdgeCases.NameCollisionTests.NumericState), 
                typeof(Features.EdgeCases.NameCollisionTests.NumericState),
                typeof(Features.EdgeCases.NameCollisionTests.NumericTrigger), 
                typeof(Features.EdgeCases.NameCollisionTests.NumericTrigger),
                null); // TODO: Create wrapper

            Register("PayloadStateMachine",
                typeof(Machines.TestState), 
                typeof(Machines.TestState),
                typeof(Machines.TestTrigger), 
                typeof(Machines.TestTrigger),
                (api, state) => api == StateMachineWrapperFactory.ApiType.Fluent
                    ? new PayloadStateMachineFluentWrapper(state)
                    : new PayloadStateMachineLegacyWrapper(state));

            Register("SelfTransition",
                typeof(Features.Core.StateCallbackTests.SelfState), 
                typeof(Features.Core.StateCallbackTests.SelfState),
                typeof(Features.Core.StateCallbackTests.SelfTrigger), 
                typeof(Features.Core.StateCallbackTests.SelfTrigger),
                null); // TODO: Create wrapper

            Register("SingleState",
                typeof(Features.EdgeCases.EmptyMachineTests.SingleState), 
                typeof(Features.EdgeCases.EmptyMachineTests.SingleState),
                typeof(Features.EdgeCases.EmptyMachineTests.SingleTrigger), 
                typeof(Features.EdgeCases.EmptyMachineTests.SingleTrigger),
                null); // TODO: Create wrapper

            Register("Unicode",
                typeof(Features.EdgeCases.NameCollisionTests.UnicodeState), 
                typeof(Features.EdgeCases.NameCollisionTests.UnicodeState),
                typeof(Features.EdgeCases.NameCollisionTests.UnicodeTrigger), 
                typeof(Features.EdgeCases.NameCollisionTests.UnicodeTrigger),
                null); // TODO: Create wrapper

            Register("Unreachable",
                typeof(Features.EdgeCases.EmptyMachineTests.UnreachableState), 
                typeof(Features.EdgeCases.EmptyMachineTests.UnreachableState),
                typeof(Features.EdgeCases.EmptyMachineTests.UnreachableTrigger), 
                typeof(Features.EdgeCases.EmptyMachineTests.UnreachableTrigger),
                null); // TODO: Create wrapper

            Register("WithGuardBenchmark",
                typeof(BenchmarkTests.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkState),
                typeof(BenchmarkTests.BenchmarkTrigger), typeof(BenchmarkTestsLegacy.BenchmarkTrigger),
                null); // TODO: Create wrapper
        }

        public static void Register(string name, 
            Type? fluentStateType, Type? legacyStateType,
            Type? fluentTriggerType, Type? legacyTriggerType,
            Func<StateMachineWrapperFactory.ApiType, string, IStateMachineTestWrapper>? wrapperFactory)
        {
            _machines[name] = new MachineInfo
            {
                Name = name,
                FluentStateType = fluentStateType,
                LegacyStateType = legacyStateType,
                FluentTriggerType = fluentTriggerType,
                LegacyTriggerType = legacyTriggerType,
                WrapperFactory = wrapperFactory
            };
        }

        public static MachineInfo? GetMachineInfo(string name)
        {
            return _machines.TryGetValue(name, out var info) ? info : null;
        }

        public static IEnumerable<MachineInfo> GetAllMachines()
        {
            return _machines.Values;
        }

        public static IEnumerable<string> GetMachineNames()
        {
            return _machines.Keys;
        }

        public static IEnumerable<MachineInfo> GetCompleteMachines()
        {
            return _machines.Values.Where(m => m.IsComplete);
        }

        public static IEnumerable<MachineInfo> GetIncompleteMachines()
        {
            return _machines.Values.Where(m => !m.IsComplete);
        }
    }
}