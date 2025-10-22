using Abstractions.Fluent;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(GuardedState), typeof(GuardedTrigger))]
public partial class GuardedCallbackMachineFluentAPI
{
    public bool AllowTransition { get; set; }
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State(GuardedState.A)
        .OnEntry(nameof(OnEntryA))
        .OnExit(nameof(OnExitA))
        .On(GuardedTrigger.Go)
        .Guard(nameof(CanTransition))
        .GoTo(GuardedState.B)
        .State(GuardedState.B)
        .OnEntry(nameof(OnEntryB));

    private bool CanTransition() => AllowTransition;
    private void OnEntryA() => EventLog.Add("OnEntry-A");
    private void OnExitA() => EventLog.Add("OnExit-A");
    private void OnEntryB() => EventLog.Add("OnEntry-B");
}