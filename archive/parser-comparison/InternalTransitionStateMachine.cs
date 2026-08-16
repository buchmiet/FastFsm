using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(ItState), typeof(ItTrigger))]
public partial class InternalTransitionStateMachine
{
    public enum ItState { S1 }
    public enum ItTrigger { Ping }

    [InternalTransition(ItState.S1, ItTrigger.Ping, Action = nameof(PingAction))]
    private void Configure() { }

    private void PingAction() { }
}

