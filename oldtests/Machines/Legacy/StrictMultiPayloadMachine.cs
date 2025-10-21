using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;

namespace FastFsm.Tests.Machines.Legacy;

public class ExpectedPayload
{
    public string Data { get; set; }
}

[StateMachine(typeof(StrictState), typeof(StrictTrigger))]
[PayloadType(StrictTrigger.Process, typeof(ExpectedPayload))]
public partial class StrictMultiPayloadMachine
{
    [Transition(StrictState.Ready, StrictTrigger.Process, StrictState.Processing)]
    private void Configure() { }
}