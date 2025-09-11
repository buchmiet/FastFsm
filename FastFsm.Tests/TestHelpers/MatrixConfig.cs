using System;
using System.Collections.Generic;
using FastFsm.Contracts;

namespace FastFsm.Tests.TestHelpers
{
    /// <summary>
    /// Central configuration for matrix tests - defines valid machines and their test scenarios
    /// </summary>
    public static class MatrixConfig
    {
        public class MachineTestConfig
        {
            public string MachineName { get; set; } = "";
            public string InitialState { get; set; } = "";
            public string[] TriggerSequence { get; set; } = Array.Empty<string>();
            public object?[] Payloads { get; set; } = Array.Empty<object?>();
            public bool RequiresAsync { get; set; }
        }

        public class MatrixEntry
        {
            public string MachineName { get; }
            public string? InitialState { get; }
            public ApiCapabilities Capabilities { get; }

            public MatrixEntry(string machineName, string? initialState, ApiCapabilities capabilities)
            {
                MachineName = machineName;
                InitialState = initialState;
                Capabilities = capabilities;
            }
        }

        /// <summary>
        /// Configuration for all machines included in matrix tests
        /// </summary>
        public static readonly IReadOnlyDictionary<string, MachineTestConfig> Machines = new Dictionary<string, MachineTestConfig>
        {
            // Core machines
            // CoreBenchmark is excluded - it's a performance test machine
            /*["CoreBenchmark"] = new MachineTestConfig
            {
                MachineName = "CoreBenchmark",
                InitialState = "A",
                TriggerSequence = new[] { "Next", "Next", "Previous" }
            },*/
            
            ["GuardPermitted"] = new MachineTestConfig
            {
                MachineName = "GuardPermitted",
                InitialState = null, // Will use fallback (Idle or first enum value)
                TriggerSequence = new[] { "Run" }
            },

            // Payload machines
            ["PayloadStateMachine"] = new MachineTestConfig
            {
                MachineName = "PayloadStateMachine",
                InitialState = null, // Will use fallback
                TriggerSequence = new[] { "Start", "Process", "Complete" },
                Payloads = new object?[] { new { OrderId = "TEST-001", Amount = 100.50m }, null }
            },

            ["FullMultiPayload"] = new MachineTestConfig
            {
                MachineName = "FullMultiPayload",
                InitialState = null, // Will use fallback
                TriggerSequence = new[] { "Configure", "Process" },
                Payloads = new object?[] { "TestData", 42 }
            },

            // Internal transition machines
            ["InternalTransition"] = new MachineTestConfig
            {
                MachineName = "InternalTransition",
                InitialState = null, // Will use fallback - Active will be resolved
                TriggerSequence = new[] { "Update", "Deactivate" }
            },

            // Exception handling machines
            ["ExceptionCallback"] = new MachineTestConfig
            {
                MachineName = "ExceptionCallback",
                InitialState = null, // Will use fallback - A will be resolved
                TriggerSequence = new[] { "Go" }
            },

            // HSM machines - only include if both Fluent and Legacy exist
            // For now, excluding HSM from matrix until Legacy wrappers are implemented
            // Uncomment these when HSM Legacy wrappers are ready:
            /*
            ["SimpleParentChild"] = new MachineTestConfig
            {
                MachineName = "SimpleParentChild",
                InitialState = "Idle",
                TriggerSequence = new[] { "Start", "Next", "Stop" }
            },

            ["DeepHistory"] = new MachineTestConfig
            {
                MachineName = "DeepHistory",
                InitialState = "Out",
                TriggerSequence = new[] { "EnterWork", "Next", "Abort", "EnterWork" }
            },

            ["ShallowHistory"] = new MachineTestConfig
            {
                MachineName = "ShallowHistory",
                InitialState = "Outside",
                TriggerSequence = new[] { "Enter", "Next", "Exit", "Enter" }
            },

            ["InitialChild"] = new MachineTestConfig
            {
                MachineName = "InitialChild",
                InitialState = "Outside",
                TriggerSequence = new[] { "EnterParent", "Switch", "LeaveParent" }
            },
            */
        };

        /// <summary>
        /// Get test configuration for a specific machine
        /// </summary>
        public static MachineTestConfig? GetConfig(string machineName)
        {
            return Machines.TryGetValue(machineName, out var config) ? config : null;
        }

        /// <summary>
        /// Get all machine names that should be included in matrix tests
        /// </summary>
        public static IEnumerable<string> GetAllMachineNames()
        {
            return Machines.Keys;
        }

        /// <summary>
        /// Create a dummy payload for testing
        /// </summary>
        public static object CreateDummyPayload()
        {
            return new { TestData = "DummyPayload", Timestamp = DateTime.UtcNow };
        }

        /// <summary>
        /// Matrix entries for parity testing - defines machines and their capabilities
        /// </summary>
        public static readonly List<MatrixEntry> MatrixEntries = new List<MatrixEntry>
        {
            // CoreBenchmark removed - it's a performance test machine
            new MatrixEntry("GuardPermitted", null, ApiCapabilities.None), // Use fallback
            new MatrixEntry("PayloadStateMachine", null, ApiCapabilities.HasDefaultPayload), // Use fallback
            new MatrixEntry("FullMultiPayload", null, ApiCapabilities.HasMultiPayloads), // Use fallback - only HasMultiPayloads
            new MatrixEntry("InternalTransition", null, ApiCapabilities.HasInternalTransitions), // Use fallback
            new MatrixEntry("ExceptionCallback", null, ApiCapabilities.HasAsync | ApiCapabilities.RequiresAsyncPath), // Has async capabilities
            
            // HSM machines
            new MatrixEntry("SimpleParentChild", null, ApiCapabilities.IsHierarchical), // Use fallback
            new MatrixEntry("DeepHistory", null, ApiCapabilities.IsHierarchical), // Use fallback - no history capability flag exists
            new MatrixEntry("ShallowHistory", null, ApiCapabilities.IsHierarchical), // Use fallback - no history capability flag exists
            new MatrixEntry("InitialChild", null, ApiCapabilities.IsHierarchical), // Use fallback
        };
    }
}