using Abstractions.Attributes;

namespace ParserComparison.Tests
{
    // Legacy version for comparison
    [StateMachine(typeof(TestState), typeof(TestTrigger), EnableHierarchy = true)]
    public partial class SimpleParentChildFluentV2LegacyComparison
    {
        [State(typeof(TestState))]
        public void ConfigureStates(IStateBuilder builder)
        {
            // Root states
            builder.DefineState(TestState.Idle);
            
            builder.DefineState(TestState.Working)
                .WithInitialChild(TestState.Working_Initializing)
                .OnEntry(OnWorkingEntry)
                .OnExit(OnWorkingExit);
                
            builder.DefineState(TestState.Completed);
            builder.DefineState(TestState.Error);
            
            // Working substates (hierarchy by naming convention)
            builder.DefineState(TestState.Working_Initializing)
                .AsChildOf(TestState.Working)
                .OnEntry(OnInitializingEntry)
                .OnExit(OnInitializingExit);
                
            builder.DefineState(TestState.Working_Processing)
                .AsChildOf(TestState.Working)
                .OnEntry(OnProcessingEntry);
                
            builder.DefineState(TestState.Working_Validating)
                .AsChildOf(TestState.Working);
        }
        
        [Transition(TestState.Idle, TestTrigger.Start)]
        public TestState OnStart() => TestState.Working;
        
        [Transition(TestState.Working_Initializing, TestTrigger.Process)]
        public TestState OnProcess() => TestState.Working_Processing;
        
        [Transition(TestState.Working_Processing, TestTrigger.Validate)]
        public TestState OnValidate() => TestState.Working_Validating;
        
        [Transition(TestState.Working, TestTrigger.Complete)]
        public TestState OnComplete() => TestState.Completed;
        
        [Transition(TestState.Working, TestTrigger.Abort)]
        public TestState OnAbort() => TestState.Error;
        
        private void OnWorkingEntry() { }
        private void OnWorkingExit() { }
        private void OnInitializingEntry() { }
        private void OnInitializingExit() { }
        private void OnProcessingEntry() { }
    }
}