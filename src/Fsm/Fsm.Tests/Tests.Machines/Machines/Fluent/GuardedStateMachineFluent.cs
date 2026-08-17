using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
public partial class GuardedStateMachine
{
    public bool CanProcess { get; set; } = true;

    private void Configure() => FSM
        .State(ProcessState.Idle)
        .On(ProcessTrigger.Start)
        .Guard(nameof(CheckCanProcess))
        .GoTo(ProcessState.Running);

    private bool CheckCanProcess() => CanProcess;
}