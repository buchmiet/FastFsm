using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Payloads;

namespace FastFsm.Tests.Machines.Legacy;

public class PermittedTriggersPayload
{
    public int Id { get; set; }
}

[StateMachine(typeof(PermittedState), typeof(PermittedTrigger))]
[PayloadType(typeof(DefaultPayload))]
public partial class PermittedTriggersMachine
{
    [Transition(PermittedState.A, PermittedTrigger.Next, PermittedState.B)]
    [Transition(PermittedState.A, PermittedTrigger.Skip, PermittedState.C)]
    [Transition(PermittedState.B, PermittedTrigger.Next, PermittedState.C)]
    private void Configure() { }
}