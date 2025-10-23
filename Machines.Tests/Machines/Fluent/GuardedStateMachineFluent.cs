using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
public partial class GuardedStateMachine
{
    public bool CanProcess { get; set; } = true;

    private static void Configure() => FSM
        .State(ProcessState.Idle)
        .On(ProcessTrigger.Start)
        .If(nameof(CheckCanProcess))
        .GoTo(ProcessState.Running);

    private bool CheckCanProcess() => CanProcess;
}