using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(TestSimpleLegacyMachine.LegacyTestState), typeof(TestSimpleLegacyMachine.LegacyTestTrigger))]
public partial class TestSimpleLegacyMachine
{
    public enum LegacyTestState { Idle, Active, Done }
    public enum LegacyTestTrigger { Start, Stop, Reset }

    public int TransitionCount { get; private set; }

    [Transition(LegacyTestState.Idle, LegacyTestTrigger.Start, LegacyTestState.Active, Action = nameof(IncrementCounter))]
    private void IdleToActive() { }

    [Transition(LegacyTestState.Active, LegacyTestTrigger.Stop, LegacyTestState.Done, Action = nameof(IncrementCounter))]
    private void ActiveToDone() { }

    [Transition(LegacyTestState.Active, LegacyTestTrigger.Reset, LegacyTestState.Idle)]
    private void ActiveToIdle() { }

    [Transition(LegacyTestState.Done, LegacyTestTrigger.Reset, LegacyTestState.Idle)]
    private void DoneToIdle() { }

    public void IncrementCounter() => TransitionCount++;
}