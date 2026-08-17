using System;
using System.Collections.Generic;

namespace Tests.Logging.TestHelpers
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
            ["PureStateMachine"] = new EnumTypePair(typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestTrigger), typeof(Tests.Logging.TestTrigger)),
            ["BasicStateMachine"] = new EnumTypePair(typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestTrigger), typeof(Tests.Logging.TestTrigger)),
            ["PayloadStateMachine"] = new EnumTypePair(typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestTrigger), typeof(Tests.Logging.TestTrigger)),
            ["ExtensionsStateMachine"] = new EnumTypePair(typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestTrigger), typeof(Tests.Logging.TestTrigger)),
            ["FullStateMachine"] = new EnumTypePair(typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestTrigger), typeof(Tests.Logging.TestTrigger)),
            ["MultiPayloadStateMachine"] = new EnumTypePair(typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestState), typeof(Tests.Logging.TestTrigger), typeof(Tests.Logging.TestTrigger)),

            // SpecialCasesLoggingTests
            ["InternalTransitionMachine"] = new EnumTypePair(typeof(Tests.Logging.InternalState), typeof(Tests.Logging.InternalState), typeof(Tests.Logging.InternalTrigger), typeof(Tests.Logging.InternalTrigger)),
            ["StructStateMachine"] = new EnumTypePair(typeof(Tests.Logging.StructState), typeof(Tests.Logging.StructState), typeof(Tests.Logging.StructTrigger), typeof(Tests.Logging.StructTrigger)),

            // LifecycleLoggingTests
            ["LifecycleMachine"] = new EnumTypePair(typeof(Tests.Logging.LifecycleState), typeof(Tests.Logging.LifecycleState), typeof(Tests.Logging.LifecycleTrigger), typeof(Tests.Logging.LifecycleTrigger)),
            ["AsyncLifecycleMachine"] = new EnumTypePair(typeof(Tests.Logging.AsyncLifecycleState), typeof(Tests.Logging.AsyncLifecycleState), typeof(Tests.Logging.AsyncLifecycleTrigger), typeof(Tests.Logging.AsyncLifecycleTrigger)),

            // LoggingExamples
            ["ExampleStateMachine"] = new EnumTypePair(typeof(Tests.Logging.OrderState), typeof(Tests.Logging.OrderState), typeof(Tests.Logging.OrderTrigger), typeof(Tests.Logging.OrderTrigger)),
            ["GuardedStateMachine"] = new EnumTypePair(typeof(Tests.Logging.ProcessState), typeof(Tests.Logging.ProcessState), typeof(Tests.Logging.ProcessTrigger), typeof(Tests.Logging.ProcessTrigger)),
            ["ExtensibleMachine"] = new EnumTypePair(typeof(Tests.Logging.WorkflowState), typeof(Tests.Logging.WorkflowState), typeof(Tests.Logging.WorkflowTrigger), typeof(Tests.Logging.WorkflowTrigger)),

            // HsmRuntimeLoggingTests
            ["HsmMachine"] = new EnumTypePair(typeof(Tests.Logging.HState), typeof(Tests.Logging.HState), typeof(Tests.Logging.HTrigger), typeof(Tests.Logging.HTrigger)),

            // LoggingIntegrationTests
            ["InitialOnEntryStateMachineActions"] = new EnumTypePair(typeof(Tests.Logging.TestInitialState), typeof(Tests.Logging.TestInitialState), typeof(Tests.Logging.TestInitialTrigger), typeof(Tests.Logging.TestInitialTrigger)),
            ["FullMultiPayloadMachine"] = new EnumTypePair(typeof(Tests.Logging.OrderStatePayload), typeof(Tests.Logging.OrderStatePayload), typeof(Tests.Logging.OrderTriggerPayload), typeof(Tests.Logging.OrderTriggerPayload)),
        };

        public static Type GetStateType(string machine, Api api) => Types[machine].For(api, isState: true);
        public static Type GetTriggerType(string machine, Api api) => Types[machine].For(api, isState: false);
    }
}
