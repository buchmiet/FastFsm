using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests
{
    // FluentAPI version - problematic one from tests
    [StateMachine(typeof(TestState), typeof(TestTrigger))]
    public partial class DiagnosticFluentMachine
    {
        public enum TestState { Idle, Active, Done }
        public enum TestTrigger { Start, Stop, Reset }

        public int TransitionCount { get; private set; }

        private static void Configure() => FSM
            .State(TestState.Idle)
                .On(TestTrigger.Start)
                    .Action(nameof(IncrementCounter))
                    .GoTo(TestState.Active)
            .State(TestState.Active)
                .On(TestTrigger.Stop)
                    .Action(nameof(IncrementCounter))
                    .GoTo(TestState.Done)
                .On(TestTrigger.Reset)
                    .GoTo(TestState.Idle)
            .State(TestState.Done)
                .On(TestTrigger.Reset)
                    .GoTo(TestState.Idle);

        public void IncrementCounter() => TransitionCount++;
    }
}