using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(GapsState), typeof(GapsTrigger), DefaultPayloadType = typeof(Job))]
public partial class GuardAsyncPayloadStateMachine
{
    public enum GapsState { Idle, Busy }
    public enum GapsTrigger { Start }

    [Transition(GapsState.Idle, GapsTrigger.Start, GapsState.Busy, Guard = nameof(CanStartAsync))]
    private void Configure() { }

    private async ValueTask<bool> CanStartAsync(Job job)
    {
        await Task.Yield();
        return job != null && job.Id > 0;
    }

    public sealed class Job { public int Id { get; init; } }
}

