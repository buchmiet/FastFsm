using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(PayloadState), typeof(PayloadTrigger), DefaultPayloadType = typeof(SinglePayloadFluentMachine.JobData))]
public partial class SinglePayloadFluentMachine
{
    public enum PayloadState { Idle, Running }
    public enum PayloadTrigger { Start, Update, Stop }

    public sealed class JobData
    {
        public required string Id { get; init; }
        public int Priority { get; init; }
    }

    private int _runningCount;

    private static void Configure() => FSM
        .State(PayloadState.Idle)
            .On(PayloadTrigger.Start).GoTo(PayloadState.Running)
                .Guard(nameof(CanStart)).Action(nameof(StartJob))
        .State(PayloadState.Running)
            .On(PayloadTrigger.Update).GoTo(PayloadState.Running)
                .Action(nameof(UpdateJob))
        .State(PayloadState.Running)
            .On(PayloadTrigger.Stop).GoTo(PayloadState.Idle)
                .Action(nameof(StopJob));

    private bool CanStart(JobData data) => _runningCount < 4 && data.Priority >= 0;
    private void StartJob(JobData data) { _runningCount++; }
    private void UpdateJob(JobData data) { }
    private void StopJob() { _runningCount--; }
}

