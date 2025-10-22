using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(GuardedState), typeof(GuardedTrigger))]
public partial class GuardedCallbackMachineFluent
{
    public bool AllowTransition { get; set; }
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State<GuardedState>(GuardedState.A)
        .OnEntry((OnEntryA))
        .OnExit((OnExitA))
        .On(GuardedTrigger.Go)
        .Guard((CanTransition))
        .GoTo(GuardedState.B)
        .State(GuardedState.B)
        .OnEntry((OnEntryB));

    private bool CanTransition() => AllowTransition;
    private void OnEntryA() => EventLog.Add("OnEntry-A");
    private void OnExitA() => EventLog.Add("OnExit-A");
    private void OnEntryB() => EventLog.Add("OnEntry-B");
}