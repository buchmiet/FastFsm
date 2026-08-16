using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(ApafState), typeof(ApafTrigger), DefaultPayloadType = typeof(JobData))]
public partial class AsyncPayloadActionFluentMachine
{
    public enum ApafState { Idle, Running }
    public enum ApafTrigger { Start, Stop }

    private static void Configure() => FSM
        .State(ApafState.Idle)
            .On(ApafTrigger.Start).GoTo(ApafState.Running).Action(nameof(BeginAsync))
        .State(ApafState.Running)
            .On(ApafTrigger.Stop).GoTo(ApafState.Idle).Action(nameof(EndAsync));

    private async ValueTask BeginAsync(JobData data)
    {
        await Task.Yield();
    }

    private async Task EndAsync(JobData data)
    {
        await Task.Yield();
    }

    public sealed class JobData { public int Id { get; init; } }
}

