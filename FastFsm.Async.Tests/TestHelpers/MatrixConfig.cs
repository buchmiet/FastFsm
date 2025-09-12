using System;
using System.Collections.Generic;

namespace FastFsm.Async.Tests.TestHelpers
{
    public static class MatrixConfig
    {
        public class MachineTestConfig
        {
            public string MachineName { get; set; } = "";
            public string? InitialState { get; set; }
            public string[] TriggerSequence { get; set; } = Array.Empty<string>();
            public bool RequiresAsync { get; set; } = true;
            public object?[] Payloads { get; set; } = Array.Empty<object?>();
        }

        public static readonly IReadOnlyDictionary<string, MachineTestConfig> Machines =
            new Dictionary<string, MachineTestConfig>(StringComparer.Ordinal)
            {
                ["InitialChild"] = new MachineTestConfig { MachineName = "InitialChild", InitialState = "Outside", TriggerSequence = new[] { "EnterParent", "Switch", "LeaveParent" } },
                ["ShallowHistory"] = new MachineTestConfig { MachineName = "ShallowHistory", InitialState = "Outside", TriggerSequence = new[] { "Enter", "Next", "Exit", "Enter" } },
                ["DeepHistory"] = new MachineTestConfig { MachineName = "DeepHistory", InitialState = "Out", TriggerSequence = new[] { "EnterWork", "Next", "Abort", "EnterWork" } },
                ["Internal"] = new MachineTestConfig { MachineName = "Internal", InitialState = "Parent", TriggerSequence = new[] { "Refresh" } },
                ["Priority"] = new MachineTestConfig { MachineName = "Priority", InitialState = "Parent", TriggerSequence = new[] { "Go" } },
                ["ChildOverrides"] = new MachineTestConfig { MachineName = "ChildOverrides", InitialState = "Parent", TriggerSequence = new[] { "Go" } },
                ["SourceOrderTie"] = new MachineTestConfig { MachineName = "SourceOrderTie", InitialState = "A", TriggerSequence = new[] { "Go" } },
                ["Inheritance"] = new MachineTestConfig { MachineName = "Inheritance", InitialState = "Outside", TriggerSequence = new[] { "Enter", "Next", "Leave" } },

                // Payload machines (first trigger configured with a payload where required)
                ["BasicPayload"] = new MachineTestConfig
                {
                    MachineName = "BasicPayload",
                    InitialState = "Initial",
                    TriggerSequence = new[] { "Start" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 1, Data = "P" } }
                },
                ["OverloadedPayload"] = new MachineTestConfig
                {
                    MachineName = "OverloadedPayload",
                    InitialState = "Initial",
                    TriggerSequence = new[] { "Start" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 2, Data = "P" } }
                },
                ["ExceptionPayload"] = new MachineTestConfig
                {
                    MachineName = "ExceptionPayload",
                    InitialState = "Initial",
                    TriggerSequence = new[] { "Start" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 3, Data = "P" } }
                },
                ["CanFirePayload"] = new MachineTestConfig
                {
                    MachineName = "CanFirePayload",
                    InitialState = "Initial",
                    TriggerSequence = new[] { "Start" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 10, Data = "P" } }
                },
                ["ConcurrentPayload"] = new MachineTestConfig
                {
                    MachineName = "ConcurrentPayload",
                    InitialState = "Processing",
                    TriggerSequence = new[] { "Process" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 100, Data = "P" } }
                },
                ["InitialOnEntryPayload"] = new MachineTestConfig
                {
                    MachineName = "InitialOnEntryPayload",
                    InitialState = "Initial",
                    TriggerSequence = new[] { "Start" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 5, Data = "P" } }
                },
                ["MultiPayload"] = new MachineTestConfig
                {
                    MachineName = "MultiPayload",
                    InitialState = "Ready",
                    TriggerSequence = new[] { "Configure" },
                    Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ConfigPayload { Setting = "S", Timeout = 1 } }
                },

                // Cancellation
                ["BasicToken"] = new MachineTestConfig { MachineName = "BasicToken", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["OptionalToken"] = new MachineTestConfig { MachineName = "OptionalToken", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["Cancellation"] = new MachineTestConfig { MachineName = "Cancellation", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["MixedToken"] = new MachineTestConfig { MachineName = "MixedToken", InitialState = "Initial", TriggerSequence = new[] { "Start" } },

                // Exceptions
                ["OnEntryContinue"] = new MachineTestConfig { MachineName = "OnEntryContinue", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["ActionPropagate"] = new MachineTestConfig { MachineName = "ActionPropagate", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["GuardException"] = new MachineTestConfig { MachineName = "GuardException", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["CancellationPropagation"] = new MachineTestConfig { MachineName = "CancellationPropagation", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["AsyncHandler"] = new MachineTestConfig { MachineName = "AsyncHandler", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["ExceptionContextCapture"] = new MachineTestConfig { MachineName = "ExceptionContextCapture", InitialState = "Idle", TriggerSequence = new[] { "Start" } },

                // Extensions
                ["ExtensionsSuccess"] = new MachineTestConfig { MachineName = "ExtensionsSuccess", InitialState = "A", TriggerSequence = new[] { "Next" } },
                ["ExtensionsFail"] = new MachineTestConfig { MachineName = "ExtensionsFail", InitialState = "A", TriggerSequence = new[] { "Fail" } },

                // Concurrency/Core
                ["RcMachine"] = new MachineTestConfig { MachineName = "RcMachine", InitialState = "Initial", TriggerSequence = new[] { "ToA" } },
                ["SimpleAsync"] = new MachineTestConfig { MachineName = "SimpleAsync", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
            };

        public static MachineTestConfig? GetConfig(string machineName) => Machines.TryGetValue(machineName, out var cfg) ? cfg : null;
        public static IEnumerable<string> GetAllMachineNames() => Machines.Keys;

        public static object CreateDummyPayload(string machineName)
        {
            return machineName switch
            {
                "MultiPayload" => new FastFsm.Async.Tests.Features.Payload.ConfigPayload { Setting = "Auto", Timeout = 1 },
                _ => new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 999, Data = "Auto" },
            };
        }
    }
}
