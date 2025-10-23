using Machines.Tests.Payloads;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;



[StateMachine(typeof(ConditionalState), typeof(ConditionalTrigger))]
[PayloadType(typeof(ConditionalPayload))]
public partial class ConditionalPayloadMachine
{
    [Transition(ConditionalState.Ready, ConditionalTrigger.Execute, ConditionalState.Done,
        Guard = (IsValid))]
    private void Configure() { }

    private bool IsValid(ConditionalPayload payload) => payload.IsValid;
}
