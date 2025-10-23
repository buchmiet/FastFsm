using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

[StateMachine(typeof(TestState), typeof(TestTrigger))]
public partial class PureStateMachine
{
    [Transition(TestState.Initial, TestTrigger.Start, TestState.Processing)]
    [Transition(TestState.Processing, TestTrigger.Complete, TestState.Completed)]
    private void Configure() { }
}