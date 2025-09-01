using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

[StateMachine(typeof(GapfState), typeof(GapfTrigger), DefaultPayloadType = typeof(Job))]
public partial class GuardAsyncPayloadFluentMachine
{
    public enum GapfState { Idle, Busy }
    public enum GapfTrigger { Start }

    private static void Configure() => FSM
        .State(GapfState.Idle)
            .On(GapfTrigger.Start).GoTo(GapfState.Busy).Guard(nameof(CanStartAsync));

    private async ValueTask<bool> CanStartAsync(Job job)
    {
        await Task.Yield();
        return job != null && job.Id > 0;
    }

    public sealed class Job { public int Id { get; init; } }
}

