using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(GpfState), typeof(GpfTrigger))]
[PayloadType(GpfTrigger.Submit, typeof(SubmitInfo))]
public partial class GuardPayloadFluentMachine
{
    public enum GpfState { Idle, Submitted }
    public enum GpfTrigger { Submit }

    private static void Configure() => FSM
        .State(GpfState.Idle)
            .On(GpfTrigger.Submit).GoTo(GpfState.Submitted).Guard(nameof(CanSubmit));

    private bool CanSubmit(SubmitInfo info) => info != null && info.Count > 0;

    public sealed class SubmitInfo { public int Count { get; init; } }
}

