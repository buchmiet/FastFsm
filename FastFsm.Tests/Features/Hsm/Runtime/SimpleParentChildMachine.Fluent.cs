using System.Collections.Generic;
using Abstractions.Attributes;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version of SimpleParentChildMachine
    [StateMachine(typeof(SimpleParentChildMachineFluent.S), typeof(SimpleParentChildMachineFluent.T), EnableHierarchy = true)]
    public partial class SimpleParentChildMachineFluent
    {
        public enum S { Idle, Working, Working_Initializing, Working_Processing, Working_Validating, Completed, Error }
        public enum T { Start, Process, Validate, Complete, Abort }
        
        public static void Configure()
        {
            // Simple states  
            FSM.State(S.Idle);
            
            // Parent state with children
            FSM.State(S.Working)
               .Initial(S.Working_Initializing)
               .OnEntry(nameof(OnWorkingEntry))
               .OnExit(nameof(OnWorkingExit));
            
            // Child states with proper hierarchy
            FSM.State(S.Working_Initializing)
               .ChildOf(S.Working)
               .OnEntry(nameof(OnInitializingEntry))
               .OnExit(nameof(OnInitializingExit));
               
            FSM.State(S.Working_Processing)
               .ChildOf(S.Working)
               .OnEntry(nameof(OnProcessingEntry));
               
            FSM.State(S.Working_Validating)
               .ChildOf(S.Working);

            // Other states
            FSM.State(S.Completed);
            FSM.State(S.Error);

            // Transitions
            FSM.State(S.Idle)
               .On(T.Start).GoTo(S.Working);
               
            FSM.State(S.Working_Initializing)
               .On(T.Process).GoTo(S.Working_Processing);
               
            FSM.State(S.Working_Processing)
               .On(T.Validate).GoTo(S.Working_Validating);
               
            FSM.State(S.Working)
               .On(T.Complete).GoTo(S.Completed)
               .On(T.Abort).GoTo(S.Error);
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

}