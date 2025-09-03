using System.Collections.Generic;
using Abstractions.Attributes;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version of SimpleParentChildMachine
    [StateMachine(typeof(HsmState_Fluent), typeof(HsmTrigger_Fluent), EnableHierarchy = true)]
    public partial class SimpleParentChildMachine_Fluent
    {
        public static void Configure()
        {
            // Simple states  
            FSM.State(HsmState_Fluent.Idle);
            
            // Parent state with children
            FSM.State(HsmState_Fluent.Working)
               .Initial(HsmState_Fluent.Working_Initializing)
               .OnEntry(nameof(OnWorkingEntry))
               .OnExit(nameof(OnWorkingExit));
            
            // Child states with proper hierarchy
            FSM.State(HsmState_Fluent.Working_Initializing)
               .ChildOf(HsmState_Fluent.Working)
               .OnEntry(nameof(OnInitializingEntry))
               .OnExit(nameof(OnInitializingExit));
               
            FSM.State(HsmState_Fluent.Working_Processing)
               .ChildOf(HsmState_Fluent.Working)
               .OnEntry(nameof(OnProcessingEntry));
               
            FSM.State(HsmState_Fluent.Working_Validating)
               .ChildOf(HsmState_Fluent.Working);

            // Other states
            FSM.State(HsmState_Fluent.Completed);
            FSM.State(HsmState_Fluent.Error);

            // Transitions
            FSM.State(HsmState_Fluent.Idle)
               .On(HsmTrigger_Fluent.Start).GoTo(HsmState_Fluent.Working);
               
            FSM.State(HsmState_Fluent.Working_Initializing)
               .On(HsmTrigger_Fluent.Process).GoTo(HsmState_Fluent.Working_Processing);
               
            FSM.State(HsmState_Fluent.Working_Processing)
               .On(HsmTrigger_Fluent.Validate).GoTo(HsmState_Fluent.Working_Validating);
               
            FSM.State(HsmState_Fluent.Working)
               .On(HsmTrigger_Fluent.Complete).GoTo(HsmState_Fluent.Completed)
               .On(HsmTrigger_Fluent.Abort).GoTo(HsmState_Fluent.Error);
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
    public enum HsmState_Fluent
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

    public enum HsmTrigger_Fluent
    {
        Start,
        Process,
        Validate,
        Complete,
        Abort
    }
}