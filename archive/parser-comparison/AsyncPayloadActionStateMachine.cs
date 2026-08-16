using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(ApasState), typeof(ApasTrigger), DefaultPayloadType = typeof(JobData))]
public partial class AsyncPayloadActionStateMachine
{
    public enum ApasState { Idle, Running }
    public enum ApasTrigger { Start, Stop }

    [Transition(ApasState.Idle, ApasTrigger.Start, ApasState.Running, Action = nameof(BeginAsync))]
    [Transition(ApasState.Running, ApasTrigger.Stop, ApasState.Idle, Action = nameof(EndAsync))]
    private void Configure() { }

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

