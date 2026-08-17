using Tests.Machines.Payloads;
using Abstractions.Attributes;

namespace Tests.Machines.Machines.Legacy;



[StateMachine(typeof(PermittedState), typeof(PermittedTrigger))]
[PayloadType(typeof(DefaultPayload))]
public partial class PermittedTriggersMachine
{
    [Transition(PermittedState.A, PermittedTrigger.Next, PermittedState.B)]
    [Transition(PermittedState.A, PermittedTrigger.Skip, PermittedState.C)]
    [Transition(PermittedState.B, PermittedTrigger.Next, PermittedState.C)]
    private void Configure() { }
}
