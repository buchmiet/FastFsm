using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version using Abstractions.Fluent (matching working pattern)
    [StateMachine(typeof(HsmState_Fluent_v2), typeof(HsmTrigger_Fluent_v2), EnableHierarchy = true)]
    public partial class SimpleParentChildMachine_Fluent_v2
    {
        // Match the exact pattern from InternalTransitionMachine
        private static void Configure() => FSM
            // Define all states first
            .State(HsmState_Fluent_v2.Idle)
            .State(HsmState_Fluent_v2.Working)
                .OnEntry(nameof(OnWorkingEntry))
                .OnExit(nameof(OnWorkingExit))
            .State(HsmState_Fluent_v2.Working_Initializing)
                .OnEntry(nameof(OnInitializingEntry))
                .OnExit(nameof(OnInitializingExit))
            .State(HsmState_Fluent_v2.Working_Processing)
                .OnEntry(nameof(OnProcessingEntry))
            .State(HsmState_Fluent_v2.Working_Validating)
            .State(HsmState_Fluent_v2.Completed)
            .State(HsmState_Fluent_v2.Error)
            
            // Define transitions
            .State(HsmState_Fluent_v2.Idle)
                .On(HsmTrigger_Fluent_v2.Start).GoTo(HsmState_Fluent_v2.Working)
            .State(HsmState_Fluent_v2.Working_Initializing)
                .On(HsmTrigger_Fluent_v2.Process).GoTo(HsmState_Fluent_v2.Working_Processing)
            .State(HsmState_Fluent_v2.Working_Processing)
                .On(HsmTrigger_Fluent_v2.Validate).GoTo(HsmState_Fluent_v2.Working_Validating)
            .State(HsmState_Fluent_v2.Working)
                .On(HsmTrigger_Fluent_v2.Complete).GoTo(HsmState_Fluent_v2.Completed)
                .On(HsmTrigger_Fluent_v2.Abort).GoTo(HsmState_Fluent_v2.Error);
        
        // Entry/Exit callbacks
        private void OnWorkingEntry() => EntryExitLog.Add("Working:Entry");
        private void OnWorkingExit() => EntryExitLog.Add("Working:Exit");
        private void OnInitializingEntry() => EntryExitLog.Add("Initializing:Entry");
        private void OnInitializingExit() => EntryExitLog.Add("Initializing:Exit");
        private void OnProcessingEntry() => EntryExitLog.Add("Processing:Entry");
        
        // Track entry/exit calls for testing
        public List<string> EntryExitLog { get; } = new List<string>();
    }

    // Enums for v2
    public enum HsmState_Fluent_v2
    {
        // Root states
        Idle,
        Working,
        Completed,
        Error,

        // Working substates (hierarchy by naming convention)
        Working_Initializing,
        Working_Processing,
        Working_Validating,
    }

    public enum HsmTrigger_Fluent_v2
    {
        Start,
        Process,
        Validate,
        Complete,
        Abort
    }
}