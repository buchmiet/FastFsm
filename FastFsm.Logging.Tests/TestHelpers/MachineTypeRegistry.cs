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
            ["InternalTransitionMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.InternalState), typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.InternalState), typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.InternalTrigger), typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.InternalTrigger)),
            ["StructStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.StructState), typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.StructState), typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.StructTrigger), typeof(FastFsm.Logging.Tests.SpecialCasesLoggingTests.StructTrigger)),

            // LifecycleLoggingTests
            ["LifecycleMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.LifecycleState), typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.LifecycleState), typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.LifecycleTrigger), typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.LifecycleTrigger)),
            ["AsyncLifecycleMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.AsyncLifecycleState), typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.AsyncLifecycleState), typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.AsyncLifecycleTrigger), typeof(FastFsm.Logging.Tests.LifecycleLoggingTests.AsyncLifecycleTrigger)),

            // LoggingExamples
            ["ExampleStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LoggingExamples.OrderState), typeof(FastFsm.Logging.Tests.LoggingExamples.OrderState), typeof(FastFsm.Logging.Tests.LoggingExamples.OrderTrigger), typeof(FastFsm.Logging.Tests.LoggingExamples.OrderTrigger)),
            ["GuardedStateMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LoggingExamples.ProcessState), typeof(FastFsm.Logging.Tests.LoggingExamples.ProcessState), typeof(FastFsm.Logging.Tests.LoggingExamples.ProcessTrigger), typeof(FastFsm.Logging.Tests.LoggingExamples.ProcessTrigger)),
            ["ExtensibleMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LoggingExamples.WorkflowState), typeof(FastFsm.Logging.Tests.LoggingExamples.WorkflowState), typeof(FastFsm.Logging.Tests.LoggingExamples.WorkflowTrigger), typeof(FastFsm.Logging.Tests.LoggingExamples.WorkflowTrigger)),

            // HsmRuntimeLoggingTests
            ["HsmMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HState), typeof(FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HState), typeof(FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HTrigger), typeof(FastFsm.Logging.Tests.HsmRuntimeLoggingTests.HTrigger)),

            // LoggingIntegrationTests
            ["InitialOnEntryStateMachineActions"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.TestInitialState), typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.TestInitialState), typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.TestInitialTrigger), typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.TestInitialTrigger)),
            ["FullMultiPayloadMachine"] = new EnumTypePair(typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.OrderStatePayload), typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.OrderStatePayload), typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.OrderTriggerPayload), typeof(FastFsm.Logging.Tests.LoggingIntegrationTests.OrderTriggerPayload)),
        };

        public static Type GetStateType(string machine, Api api) => Types[machine].For(api, isState: true);
        public static Type GetTriggerType(string machine, Api api) => Types[machine].For(api, isState: false);
    }
}

