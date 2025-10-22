using Abstractions.Fluent;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(ComplexCallbackState), typeof(ComplexCallbackTrigger))]
public partial class ComplexCallbackMachineFluentAPI
{
    public List<string> EventSequence { get; } = [];
    public bool ResourcesCleaned { get; private set; }
    public DateTime? CompletionTime { get; private set; }

    private void Configure() => FSM
        .State(ComplexCallbackState.Idle)
        .OnEntry((OnEnterIdle))
        .OnExit((OnExitIdle))
        .On(ComplexCallbackTrigger.Start)
        .GoTo(ComplexCallbackState.Ready)
        .State(ComplexCallbackState.Ready)
        .OnEntry((OnEnterReady))
        .OnExit((OnExitReady))
        .On(ComplexCallbackTrigger.Process)
        .GoTo(ComplexCallbackState.Processing)
        .State(ComplexCallbackState.Processing)
        .OnEntry((OnEnterProcessing))
        .OnExit((OnExitProcessing))
        .On(ComplexCallbackTrigger.Complete)
        .GoTo(ComplexCallbackState.Done)
        .State(ComplexCallbackState.Done)
        .OnEntry((OnEnterDone));

    private void OnEnterIdle() => EventSequence.Add("Entry-Idle");
    private void OnExitIdle() => EventSequence.Add("Exit-Idle");
    private void OnEnterReady() => EventSequence.Add("Entry-Ready");
    private void OnExitReady() => EventSequence.Add("Exit-Ready");
    private void OnEnterProcessing() => EventSequence.Add("Entry-Processing");
    private void OnExitProcessing()
    {
        EventSequence.Add("Exit-Processing");
        ResourcesCleaned = true;
    }
    private void OnEnterDone()
    {
        EventSequence.Add("Entry-Done");
        CompletionTime = DateTime.Now;
    }
}