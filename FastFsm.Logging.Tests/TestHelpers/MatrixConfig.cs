using System;
using System.Collections.Generic;

namespace FastFsm.Logging.Tests.TestHelpers
{
    public static class MatrixConfig
    {
        public class MachineTestConfig
        {
            public string MachineName { get; set; } = "";
            public string? InitialState { get; set; }
            public string[] TriggerSequence { get; set; } = Array.Empty<string>();
            public object?[] Payloads { get; set; } = Array.Empty<object?>();
        }

        public static readonly IReadOnlyDictionary<string, MachineTestConfig> Machines =
            new Dictionary<string, MachineTestConfig>(StringComparer.Ordinal)
            {
                // Start od maszyn z Machines.cs + HSM (prosty smoke)
                ["PureStateMachine"] = new MachineTestConfig { MachineName = "PureStateMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["BasicStateMachine"] = new MachineTestConfig { MachineName = "BasicStateMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["PayloadStateMachine"] = new MachineTestConfig { MachineName = "PayloadStateMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Logging.Tests.TestPayload() } },
                ["ExtensionsStateMachine"] = new MachineTestConfig { MachineName = "ExtensionsStateMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["FullStateMachine"] = new MachineTestConfig { MachineName = "FullStateMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Logging.Tests.TestPayload() } },
                ["MultiPayloadStateMachine"] = new MachineTestConfig { MachineName = "MultiPayloadStateMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Logging.Tests.TestPayload() } },

                ["HsmMachine"] = new MachineTestConfig { MachineName = "HsmMachine", InitialState = "A", TriggerSequence = new[] { "Refresh" } },
                
                // Lifecycle machines
                ["LifecycleMachine"] = new MachineTestConfig { MachineName = "LifecycleMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["AsyncLifecycleMachine"] = new MachineTestConfig { MachineName = "AsyncLifecycleMachine", InitialState = "Initial", TriggerSequence = new[] { "StartAsync" } },
                
                // Special cases
                ["InternalTransitionMachine"] = new MachineTestConfig { MachineName = "InternalTransitionMachine", InitialState = "Active", TriggerSequence = new[] { "Refresh" } },
                ["StructStateMachine"] = new MachineTestConfig { MachineName = "StructStateMachine", InitialState = "One", TriggerSequence = new[] { "Next" } },
                
                // Integration machines
                ["InitialOnEntryStateMachineActions"] = new MachineTestConfig { MachineName = "InitialOnEntryStateMachineActions", InitialState = "Ready", TriggerSequence = new[] { "Go" } },
                ["FullMultiPayloadMachine"] = new MachineTestConfig { MachineName = "FullMultiPayloadMachine", InitialState = "New", TriggerSequence = new[] { "Process" }, Payloads = new object?[] { new FastFsm.Logging.Tests.OrderPayload { OrderId = 1001, Amount = 100.50m } } },
                
                // Example machines
                ["ExampleStateMachine"] = new MachineTestConfig { MachineName = "ExampleStateMachine", InitialState = "New", TriggerSequence = new[] { "Submit" } },
                ["GuardedStateMachine"] = new MachineTestConfig { MachineName = "GuardedStateMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["ExtensibleMachine"] = new MachineTestConfig { MachineName = "ExtensibleMachine", InitialState = "Draft", TriggerSequence = new[] { "Submit" } },
            };

        public static MachineTestConfig? GetConfig(string machineName) => Machines.TryGetValue(machineName, out var cfg) ? cfg : null;
        public static IEnumerable<string> GetAllMachineNames() => Machines.Keys;
    }
}
