using System;
using System.Collections.Generic;

namespace FastFsm.Logging.Tests.TestHelpers
{
    public static class MachineTypeRegistry
    {
        public enum Api { Fluent, Legacy }

        public sealed class EnumTypePair
        {
            public Type FluentState { get; }
            public Type LegacyState { get; }
            public Type FluentTrigger { get; }
            public Type LegacyTrigger { get; }
            public EnumTypePair(Type fs, Type ls, Type ft, Type lt) { FluentState = fs; LegacyState = ls; FluentTrigger = ft; LegacyTrigger = lt; }
            public Type For(Api api, bool isState) => (api, isState) switch
            {
                (Api.Fluent, true) => FluentState,
                (Api.Legacy, true) => LegacyState,
                (Api.Fluent, false) => FluentTrigger,
                (Api.Legacy, false) => LegacyTrigger,
                _ => throw new ArgumentException("Invalid combination")
            };
            public bool UsesSameEnums => FluentState == LegacyState && FluentTrigger == LegacyTrigger;
        }

        public static readonly IReadOnlyDictionary<string, EnumTypePair> Types = new Dictionary<string, EnumTypePair>(StringComparer.Ordinal)
        {
            // Machines.cs – wszystkie używają TestState/TestTrigger
            ["PureStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestTrigger), typeof(FastFsm.Logging.Tests.TestTrigger)),
            ["BasicStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestTrigger), typeof(FastFsm.Logging.Tests.TestTrigger)),
            ["PayloadStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestTrigger), typeof(FastFsm.Logging.Tests.TestTrigger)),
            ["ExtensionsStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestTrigger), typeof(FastFsm.Logging.Tests.TestTrigger)),
            ["FullStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestTrigger), typeof(FastFsm.Logging.Tests.TestTrigger)),
            ["MultiPayloadStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestState), typeof(FastFsm.Logging.Tests.TestTrigger), typeof(FastFsm.Logging.Tests.TestTrigger)),

            // SpecialCasesLoggingTests
            ["InternalTransitionMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.InternalState), typeof(FastFsm.Logging.Tests.InternalState), typeof(FastFsm.Logging.Tests.InternalTrigger), typeof(FastFsm.Logging.Tests.InternalTrigger)),
            ["StructStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.StructState), typeof(FastFsm.Logging.Tests.StructState), typeof(FastFsm.Logging.Tests.StructTrigger), typeof(FastFsm.Logging.Tests.StructTrigger)),

            // LifecycleLoggingTests
            ["LifecycleMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LifecycleState), typeof(FastFsm.Logging.Tests.LifecycleState), typeof(FastFsm.Logging.Tests.LifecycleTrigger), typeof(FastFsm.Logging.Tests.LifecycleTrigger)),
            ["AsyncLifecycleMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.AsyncLifecycleState), typeof(FastFsm.Logging.Tests.AsyncLifecycleState), typeof(FastFsm.Logging.Tests.AsyncLifecycleTrigger), typeof(FastFsm.Logging.Tests.AsyncLifecycleTrigger)),

            // LoggingExamples
            ["ExampleStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.OrderState), typeof(FastFsm.Logging.Tests.OrderState), typeof(FastFsm.Logging.Tests.OrderTrigger), typeof(FastFsm.Logging.Tests.OrderTrigger)),
            ["GuardedStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.ProcessState), typeof(FastFsm.Logging.Tests.ProcessState), typeof(FastFsm.Logging.Tests.ProcessTrigger), typeof(FastFsm.Logging.Tests.ProcessTrigger)),
            ["ExtensibleMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.WorkflowState), typeof(FastFsm.Logging.Tests.WorkflowState), typeof(FastFsm.Logging.Tests.WorkflowTrigger), typeof(FastFsm.Logging.Tests.WorkflowTrigger)),

            // HsmRuntimeLoggingTests
            ["HsmMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.HState), typeof(FastFsm.Logging.Tests.HState), typeof(FastFsm.Logging.Tests.HTrigger), typeof(FastFsm.Logging.Tests.HTrigger)),

            // LoggingIntegrationTests
            ["InitialOnEntryStateMachineActions"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.TestInitialState), typeof(FastFsm.Logging.Tests.TestInitialState), typeof(FastFsm.Logging.Tests.TestInitialTrigger), typeof(FastFsm.Logging.Tests.TestInitialTrigger)),
            ["FullMultiPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.OrderStatePayload), typeof(FastFsm.Logging.Tests.OrderStatePayload), typeof(FastFsm.Logging.Tests.OrderTriggerPayload), typeof(FastFsm.Logging.Tests.OrderTriggerPayload)),
        };

        public static Type GetStateType(string machine, Api api) => Types[machine].For(api, isState: true);
        public static Type GetTriggerType(string machine, Api api) => Types[machine].For(api, isState: false);
    }
}
