using System.Collections.Generic;
using Abstractions.Attributes;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version of SimpleParentChildMachine
    [StateMachine(typeof(HsmStateFluent), typeof(HsmTriggerFluent), EnableHierarchy = true)]
    public partial class SimpleParentChildMachineFluent
    {
        public static void Configure()
        {
            // Simple states  
            FSM.State(HsmStateFluent.Idle);
            
            // Parent state with children
            FSM.State(HsmStateFluent.Working)
               .Initial(HsmStateFluent.Working_Initializing)
               .OnEntry(nameof(OnWorkingEntry))
               .OnExit(nameof(OnWorkingExit));
            
            // Child states with proper hierarchy
            FSM.State(HsmStateFluent.Working_Initializing)
               .ChildOf(HsmStateFluent.Working)
               .OnEntry(nameof(OnInitializingEntry))
               .OnExit(nameof(OnInitializingExit));
               
            FSM.State(HsmStateFluent.Working_Processing)
               .ChildOf(HsmStateFluent.Working)
               .OnEntry(nameof(OnProcessingEntry));
               
            FSM.State(HsmStateFluent.Working_Validating)
               .ChildOf(HsmStateFluent.Working);

            // Other states
            FSM.State(HsmStateFluent.Completed);
            FSM.State(HsmStateFluent.Error);

            // Transitions
            FSM.State(HsmStateFluent.Idle)
               .On(HsmTriggerFluent.Start).GoTo(HsmStateFluent.Working);
               
            FSM.State(HsmStateFluent.Working_Initializing)
               .On(HsmTriggerFluent.Process).GoTo(HsmStateFluent.Working_Processing);
               
            FSM.State(HsmStateFluent.Working_Processing)
               .On(HsmTriggerFluent.Validate).GoTo(HsmStateFluent.Working_Validating);
               
            FSM.State(HsmStateFluent.Working)
               .On(HsmTriggerFluent.Complete).GoTo(HsmStateFluent.Completed)
               .On(HsmTriggerFluent.Abort).GoTo(HsmStateFluent.Error);
        }
        
        // Entry/Exit callbacks
        public void OnWorkingEntry() => EntryExitLog.Add("Working:Entry");
        public void OnWorkingExit() => EntryExitLog.Add("Working:Exit");
        public void OnInitializingEntry() => EntryExitLog.Add("Initializing:Entry");
        public void OnInitializingExit() => EntryExitLog.Add("Initializing:Exit");
        public void OnProcessingEntry() => EntryExitLog.Add("Processing:Entry");
        
        // Track entry/exit calls for testing
        public List<string> EntryExitLog { get; } = new List<string>();
    }

    // Enums for Fluent version
    public enum HsmStateFluent
    {
        // Root states
        Idle,
        Working,
        Completed,
        Error,

        // Working substates
        Working_Initializing,
        Working_Processing,
        Working_Validating,
    }

    public enum HsmTriggerFluent
    {
        Start,
        Process,
        Validate,
        Complete,
        Abort
    }
}