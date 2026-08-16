using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

public class ExpectedPayload
{
    public string Data { get; set; } = null!;
}

[StateMachine(typeof(StrictState), typeof(StrictTrigger))]
[PayloadType(StrictTrigger.Process, typeof(ExpectedPayload))]
public partial class StrictMultiPayloadMachine
{
    [Transition(StrictState.Ready, StrictTrigger.Process, StrictState.Processing)]
    private void Configure() { }
}
