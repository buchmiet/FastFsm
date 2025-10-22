using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(SelfState), typeof(SelfTrigger))]
public partial class SelfTransitionMachineFluent
{
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State(SelfState.Active)
        .OnEntry((OnEntryActive)).OnExit((OnExitActive))
        .On(SelfTrigger.Refresh).Action((RefreshAction)).GoTo(SelfState.Active);

    private void OnEntryActive() => EventLog.Add("OnEntry-Active");
    private void OnExitActive() => EventLog.Add("OnExit-Active");
    private void RefreshAction() => EventLog.Add("RefreshAction");
}