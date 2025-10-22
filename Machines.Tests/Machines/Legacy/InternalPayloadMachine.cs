using Machines.Tests.Payloads;

namespace Machines.Tests.Machines.Legacy;



[StateMachine(typeof(InternalPayloadState), typeof(InternalPayloadTrigger))]
[PayloadType(typeof(UpdatePayload))]
public partial class InternalPayloadMachine
{
    public int Counter { get; private set; }

    [InternalTransition(InternalPayloadState.Active, InternalPayloadTrigger.Update, Action = (UpdateCounter))]
    [Transition(InternalPayloadState.Active, InternalPayloadTrigger.Deactivate, InternalPayloadState.Inactive)]
    private void Configure() { }

    private void UpdateCounter(UpdatePayload update)
    {
        Counter += update.Increment;
    }
}