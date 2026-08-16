using Abstractions.Attributes;

namespace ParserComparison.Tests
{
    // Legacy attribute version - for comparison
    [StateMachine(typeof(TestState), typeof(TestTrigger))]
    public partial class DiagnosticAttributeMachine
    {
        public enum TestState { Idle, Active, Done }
        public enum TestTrigger { Start, Stop, Reset }

        public int TransitionCount { get; private set; }

        [State(TestState.Idle)]
        [State(TestState.Active)]
        [State(TestState.Done)]
        private void ConfigureStates() { }

        [Transition(TestState.Idle, TestTrigger.Start, TestState.Active, 
            Action = nameof(IncrementCounter))]
        [Transition(TestState.Active, TestTrigger.Stop, TestState.Done,
            Action = nameof(IncrementCounter))]
        [Transition(TestState.Active, TestTrigger.Reset, TestState.Idle)]
        [Transition(TestState.Done, TestTrigger.Reset, TestState.Idle)]
        private void ConfigureTransitions() { }

        public void IncrementCounter() => TransitionCount++;
    }
}