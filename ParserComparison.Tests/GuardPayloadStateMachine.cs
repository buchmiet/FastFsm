using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(GpState), typeof(GpTrigger))]
[PayloadType(GpTrigger.Submit, typeof(SubmitData))]
public partial class GuardPayloadStateMachine
{
    public enum GpState { Idle, Submitted }
    public enum GpTrigger { Submit }

    [Transition(GpState.Idle, GpTrigger.Submit, GpState.Submitted, Guard = nameof(CanSubmit))]
    private void Configure() { }

    private bool CanSubmit(SubmitData data) => data != null && data.Count > 0;

    public sealed class SubmitData { public int Count { get; init; } }
}

