using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

[StateMachine(typeof(IpfState), typeof(IpfTrigger), DefaultPayloadType = typeof(Payload))]
public partial class InternalPayloadFluentMachine
{
    public enum IpfState { S1 }
    public enum IpfTrigger { Ping }

    private static void Configure() => FSM
        .State(IpfState.S1)
            .OnInternal(IpfTrigger.Ping).Action(nameof(Ping));

    private void Ping(Payload p) { }

    public sealed class Payload { public int Id { get; init; } }
}

