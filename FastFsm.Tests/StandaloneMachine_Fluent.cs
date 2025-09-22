using Abstractions.Fluent;

namespace FastFsm.Tests;

// Non-nested FluentAPI machine for testing
[StateMachine(typeof(StandaloneState), typeof(StandaloneTrigger))]
public partial class StandaloneFluentMachine
{
    public enum StandaloneState { Idle, Working, Done }
    public enum StandaloneTrigger { Start, Complete, Reset }

    public int Counter { get; private set; }

    private void Configure() => FSM
        .State(StandaloneState.Idle)
        .On(StandaloneTrigger.Start)
        .Action(nameof(IncrementCounter))
        .GoTo(StandaloneState.Working)
        .State(StandaloneState.Working)
        .On(StandaloneTrigger.Complete)
        .Action(nameof(IncrementCounter))
        .GoTo(StandaloneState.Done)
        .State(StandaloneState.Done)
        .On(StandaloneTrigger.Reset)
        .GoTo(StandaloneState.Idle);

    public void IncrementCounter() => Counter++;
}