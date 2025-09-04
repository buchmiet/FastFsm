using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests
{
    // Test machine exactly matching SimpleParentChildMachine_Fluent_v2 from FastFsm.Tests
    [StateMachine(typeof(TestState), typeof(TestTrigger), EnableHierarchy = true)]
    public partial class SimpleParentChildFluentV2Test
    {
        private static void Configure() => FSM
            .State(TestState.Idle)
            .State(TestState.Working)
                .OnEntry(nameof(OnWorkingEntry))
                .OnExit(nameof(OnWorkingExit))
            .State(TestState.Working_Initializing)
                .OnEntry(nameof(OnInitializingEntry))
                .OnExit(nameof(OnInitializingExit))
            .State(TestState.Working_Processing)
                .OnEntry(nameof(OnProcessingEntry))
            .State(TestState.Working_Validating)
            .State(TestState.Completed)
            .State(TestState.Error)
            
            // Transitions
            .State(TestState.Idle)
                .On(TestTrigger.Start).GoTo(TestState.Working)
            .State(TestState.Working_Initializing)
                .On(TestTrigger.Process).GoTo(TestState.Working_Processing)
            .State(TestState.Working_Processing)
                .On(TestTrigger.Validate).GoTo(TestState.Working_Validating)
            .State(TestState.Working)
                .On(TestTrigger.Complete).GoTo(TestState.Completed)
                .On(TestTrigger.Abort).GoTo(TestState.Error);
        
        private void OnWorkingEntry() => EntryExitLog.Add("Working:Entry");
        private void OnWorkingExit() => EntryExitLog.Add("Working:Exit");
        private void OnInitializingEntry() => EntryExitLog.Add("Initializing:Entry");
        private void OnInitializingExit() => EntryExitLog.Add("Initializing:Exit");
        private void OnProcessingEntry() => EntryExitLog.Add("Processing:Entry");
        
        public List<string> EntryExitLog { get; } = new List<string>();
    }

    public enum TestState
    {
        Idle,
        Working,
        Completed,
        Error,
        Working_Initializing,
        Working_Processing,
        Working_Validating,
    }

    public enum TestTrigger
    {
        Start,
        Process,
        Validate,
        Complete,
        Abort
    }
}