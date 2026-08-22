using Tests.Machines.Payloads;
using Abstractions.Attributes;

namespace Tests.Machines.Machines.Legacy;



[StateMachine(typeof(InternalPayloadState), typeof(InternalPayloadTrigger))]
[PayloadType(typeof(UpdatePayload))]
public partial class InternalPayloadMachine
{
    public int Counter { get; private set; }

    [InternalTransition(InternalPayloadState.Active, InternalPayloadTrigger.Update, Action = nameof(UpdateCounter))]
    [Transition(InternalPayloadState.Active, InternalPayloadTrigger.Deactivate, InternalPayloadState.Inactive)]
    private void Configure() { }

    private void UpdateCounter(UpdatePayload update) => Counter += update.Increment;
}
