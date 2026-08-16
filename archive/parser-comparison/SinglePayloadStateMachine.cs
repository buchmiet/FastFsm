using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(PayloadState), typeof(PayloadTrigger), DefaultPayloadType = typeof(JobData))]
public partial class SinglePayloadStateMachine
{
    public enum PayloadState { Idle, Running }
    public enum PayloadTrigger { Start, Update, Stop }

    public sealed class JobData
    {
        public required string Id { get; init; }
        public int Priority { get; init; }
    }

    private int _runningCount;

    [State(PayloadState.Idle)]
    private void Idle() { }

    [State(PayloadState.Running)]
    private void Running() { }

    [Transition(PayloadState.Idle, PayloadTrigger.Start, PayloadState.Running, Guard = nameof(CanStart), Action = nameof(StartJob))]
    private void T1() { }

    [Transition(PayloadState.Running, PayloadTrigger.Update, PayloadState.Running, Action = nameof(UpdateJob))]
    private void T2() { }

    [Transition(PayloadState.Running, PayloadTrigger.Stop, PayloadState.Idle, Action = nameof(StopJob))]
    private void T3() { }

    // payload-aware guard/action signatures:
    private bool CanStart(JobData data) => _runningCount < 4 && data.Priority >= 0;
    private void StartJob(JobData data) { _runningCount++; }
    private void UpdateJob(JobData data) { }
    private void StopJob() { _runningCount--; }
}

