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

                // Add 1:1 base-name entries for full parity (aliases to scenarios above)
                ["InitialChildMachine"] = new MachineTestConfig { MachineName = "InitialChildMachine", InitialState = "Outside", TriggerSequence = new[] { "EnterParent", "Switch", "LeaveParent" } },
                ["ShallowHistoryMachine"] = new MachineTestConfig { MachineName = "ShallowHistoryMachine", InitialState = "Outside", TriggerSequence = new[] { "Enter", "Next", "Exit", "Enter" } },
                ["DeepHistoryMachine"] = new MachineTestConfig { MachineName = "DeepHistoryMachine", InitialState = "Out", TriggerSequence = new[] { "EnterWork", "Next", "Abort", "EnterWork" } },
                ["InternalMachine"] = new MachineTestConfig { MachineName = "InternalMachine", InitialState = "Parent", TriggerSequence = new[] { "Refresh" } },
                ["PriorityMachine"] = new MachineTestConfig { MachineName = "PriorityMachine", InitialState = "Parent", TriggerSequence = new[] { "Go" } },
                ["ChildOverridesMachine"] = new MachineTestConfig { MachineName = "ChildOverridesMachine", InitialState = "Parent", TriggerSequence = new[] { "Go" } },
                ["SourceOrderTieMachine"] = new MachineTestConfig { MachineName = "SourceOrderTieMachine", InitialState = "A", TriggerSequence = new[] { "Go" } },
                ["InheritanceMachine"] = new MachineTestConfig { MachineName = "InheritanceMachine", InitialState = "Outside", TriggerSequence = new[] { "Enter", "Next", "Leave" } },

                ["BasicAsyncPayloadMachine"] = new MachineTestConfig { MachineName = "BasicAsyncPayloadMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 1, Data = "P" } } },
                ["OverloadedAsyncMachine"] = new MachineTestConfig { MachineName = "OverloadedAsyncMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 2, Data = "P" } } },
                ["ExceptionAsyncPayloadMachine"] = new MachineTestConfig { MachineName = "ExceptionAsyncPayloadMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 3, Data = "P" } } },
                ["CanFireAsyncPayloadMachine"] = new MachineTestConfig { MachineName = "CanFireAsyncPayloadMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 10, Data = "P" } } },
                ["ConcurrentAsyncPayloadMachine"] = new MachineTestConfig { MachineName = "ConcurrentAsyncPayloadMachine", InitialState = "Processing", TriggerSequence = new[] { "Process" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 100, Data = "P" } } },
                ["InitialOnEntryAsyncPayloadMachine"] = new MachineTestConfig { MachineName = "InitialOnEntryAsyncPayloadMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ProcessPayload { Id = 5, Data = "P" } } },
                ["MultiPayloadAsyncMachine"] = new MachineTestConfig { MachineName = "MultiPayloadAsyncMachine", InitialState = "Ready", TriggerSequence = new[] { "Configure" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Payload.ConfigPayload { Setting = "S", Timeout = 1 } } },

                ["BasicTokenMachine"] = new MachineTestConfig { MachineName = "BasicTokenMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["OptionalTokenMachine"] = new MachineTestConfig { MachineName = "OptionalTokenMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["CancellationMachine"] = new MachineTestConfig { MachineName = "CancellationMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["MixedTokenMachine"] = new MachineTestConfig { MachineName = "MixedTokenMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },

                ["OnEntryContinueMachine"] = new MachineTestConfig { MachineName = "OnEntryContinueMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["ActionPropagateMachine"] = new MachineTestConfig { MachineName = "ActionPropagateMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["GuardExceptionMachine"] = new MachineTestConfig { MachineName = "GuardExceptionMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["CancellationPropagationMachine"] = new MachineTestConfig { MachineName = "CancellationPropagationMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["AsyncHandlerMachine"] = new MachineTestConfig { MachineName = "AsyncHandlerMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },
                ["ExceptionContextCaptureMachine"] = new MachineTestConfig { MachineName = "ExceptionContextCaptureMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },

                ["AsyncHookOrderMachineSuccess"] = new MachineTestConfig { MachineName = "AsyncHookOrderMachineSuccess", InitialState = "A", TriggerSequence = new[] { "Next" } },
                ["AsyncHookOrderMachineFail"] = new MachineTestConfig { MachineName = "AsyncHookOrderMachineFail", InitialState = "A", TriggerSequence = new[] { "Fail" } },
                ["AsyncExtensionsMachine"] = new MachineTestConfig { MachineName = "AsyncExtensionsMachine", InitialState = "Idle", TriggerSequence = new[] { "Start" } },

                ["SimpleAsyncMachine"] = new MachineTestConfig { MachineName = "SimpleAsyncMachine", InitialState = "Initial", TriggerSequence = new[] { "Start" } },
                ["RcMachine"] = new MachineTestConfig { MachineName = "RcMachine", InitialState = "Initial", TriggerSequence = new[] { "ToA" } },
                ["TokenMachine"] = new MachineTestConfig { MachineName = "TokenMachine", InitialState = "Off", TriggerSequence = new[] { "SwitchOn" } },
                ["PayloadMachine"] = new MachineTestConfig { MachineName = "PayloadMachine", InitialState = "Off", TriggerSequence = new[] { "ToggleOn" }, Payloads = new object?[] { new FastFsm.Async.Tests.Features.Cancellation.TogglePayload { Id = 7 } } },
                ["SimpleCancellationMachine"] = new MachineTestConfig { MachineName = "SimpleCancellationMachine", InitialState = "Ready", TriggerSequence = new[] { "Start" } },
                ["SpecificationComplianceMachine"] = new MachineTestConfig { MachineName = "SpecificationComplianceMachine", InitialState = "Ready", TriggerSequence = new[] { "Start" } },
                ["TinyAsyncHsm"] = new MachineTestConfig { MachineName = "TinyAsyncHsm", InitialState = "Outside", TriggerSequence = new[] { "Enter" } },
                ["ExceptionAsyncMachine"] = new MachineTestConfig { MachineName = "ExceptionAsyncMachine", InitialState = "Init", TriggerSequence = new[] { "GuardBoom" } },
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
