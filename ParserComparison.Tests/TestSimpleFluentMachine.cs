using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(TestSimpleFluentMachine.FluentTestState), typeof(TestSimpleFluentMachine.FluentTestTrigger))]
public partial class TestSimpleFluentMachine
{
    public enum FluentTestState { Idle, Active, Done }
    public enum FluentTestTrigger { Start, Stop, Reset }

    public int TransitionCount { get; private set; }

    private static void Configure() => FSM
        .State(FluentTestState.Idle)
            .On(FluentTestTrigger.Start)
                .Action(nameof(IncrementCounter))
                .GoTo(FluentTestState.Active)
        .State(FluentTestState.Active)
            .On(FluentTestTrigger.Stop)
                .Action(nameof(IncrementCounter))
                .GoTo(FluentTestState.Done)
            .On(FluentTestTrigger.Reset)
                .GoTo(FluentTestState.Idle)
        .State(FluentTestState.Done)
            .On(FluentTestTrigger.Reset)
                .GoTo(FluentTestState.Idle);

    public void IncrementCounter() => TransitionCount++;
}