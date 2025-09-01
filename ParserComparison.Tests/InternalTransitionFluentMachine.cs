using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

[StateMachine(typeof(ItfState), typeof(ItfTrigger))]
public partial class InternalTransitionFluentMachine
{
    public enum ItfState { S1 }
    public enum ItfTrigger { Ping }

    private static void Configure() => FSM
        .State(ItfState.S1)
            .OnInternal(ItfTrigger.Ping).Action(nameof(PingAction));

    private void PingAction() { }
}

