using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(IpsState), typeof(IpsTrigger), DefaultPayloadType = typeof(Payload))]
public partial class InternalPayloadStateMachine
{
    public enum IpsState { S1 }
    public enum IpsTrigger { Ping }

    [InternalTransition(IpsState.S1, IpsTrigger.Ping, Action = nameof(Ping))]
    private void Configure() { }

    private void Ping(Payload p) { }

    public sealed class Payload { public int Id { get; init; } }
}

