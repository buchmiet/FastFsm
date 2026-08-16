using Machines.Tests.Payloads;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;


[StateMachine(typeof(MixedState), typeof(MixedTrigger))]
[PayloadType(typeof(DefaultPayload))]
[PayloadType(MixedTrigger.Special, typeof(SpecialPayload))]
public partial class MixedPayloadMachine
{
    public int LastDefaultId { get; private set; }
    public string LastSpecialValue { get; private set; } = null!;

    [Transition(MixedState.Start, MixedTrigger.Regular, MixedState.Middle,
        Action = nameof(ProcessDefault))]
    [Transition(MixedState.Middle, MixedTrigger.Special, MixedState.End,
        Action = nameof(ProcessSpecial))]
    private void Configure() { }

    private void ProcessDefault(DefaultPayload payload)
    {
        LastDefaultId = payload.Id;
    }

    private void ProcessSpecial(SpecialPayload payload)
    {
        LastSpecialValue = payload.SpecialValue;
    }
}
