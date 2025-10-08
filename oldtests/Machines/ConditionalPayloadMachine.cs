using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Payloads;

namespace FastFsm.Tests.Machines;



[StateMachine(typeof(ConditionalState), typeof(ConditionalTrigger))]
[PayloadType(typeof(ConditionalPayload))]
public partial class ConditionalPayloadMachine
{
    [Transition(ConditionalState.Ready, ConditionalTrigger.Execute, ConditionalState.Done,
        Guard = nameof(IsValid))]
    private void Configure() { }

    private bool IsValid(ConditionalPayload payload) => payload.IsValid;
}