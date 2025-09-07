using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version using Abstractions.Fluent (matching working pattern)
    [StateMachine(typeof(HsmStateFluent_v2), typeof(HsmTriggerFluent_v2), EnableHierarchy = true)]
    public partial class SimpleParentChildMachineFluent_v2
    {
        // Match the exact pattern from InternalTransitionMachine
        private static void Configure() => FSM
            // Define all states first
            .State(HsmStateFluent_v2.Idle)
            .State(HsmStateFluent_v2.Working)
                // Note: Initial/ChildOf not yet in Abstractions.Fluent package
                .OnEntry(nameof(OnWorkingEntry))
                .OnExit(nameof(OnWorkingExit))
            .State(HsmStateFluent_v2.Working_Initializing)
                .OnEntry(nameof(OnInitializingEntry))
                .OnExit(nameof(OnInitializingExit))
            .State(HsmStateFluent_v2.Working_Processing)
                .OnEntry(nameof(OnProcessingEntry))
            .State(HsmStateFluent_v2.Working_Validating)
            .State(HsmStateFluent_v2.Completed)
            .State(HsmStateFluent_v2.Error)
            
            // Define transitions
            .State(HsmStateFluent_v2.Idle)
                .On(HsmTriggerFluent_v2.Start).GoTo(HsmStateFluent_v2.Working)
            .State(HsmStateFluent_v2.Working_Initializing)
                .On(HsmTriggerFluent_v2.Process).GoTo(HsmStateFluent_v2.Working_Processing)
            .State(HsmStateFluent_v2.Working_Processing)
                .On(HsmTriggerFluent_v2.Validate).GoTo(HsmStateFluent_v2.Working_Validating)
            .State(HsmStateFluent_v2.Working)
                .On(HsmTriggerFluent_v2.Complete).GoTo(HsmStateFluent_v2.Completed)
                .On(HsmTriggerFluent_v2.Abort).GoTo(HsmStateFluent_v2.Error);
        
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
    public enum HsmStateFluent_v2
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

    public enum HsmTriggerFluent_v2
    {
        Start,
        Process,
        Validate,
        Complete,
        Abort
    }
}