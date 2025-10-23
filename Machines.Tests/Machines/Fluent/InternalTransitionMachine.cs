using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(InternalState), typeof(InternalTrigger))]
public partial class InternalTransitionMachine
{
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State(InternalState.Active)
        .OnEntry((OnEntryActive)).OnExit((OnExitActive))
        .On(InternalTrigger.Deactivate).GoTo(InternalState.Inactive)
        .State(InternalState.Inactive)
        .OnEntry((OnEntryInactive))
        .State(InternalState.Active)
        .OnInternal(InternalTrigger.Update).Action((HandleUpdate));

    private void OnEntryActive() => EventLog.Add("OnEntry-Active");
    private void OnExitActive() => EventLog.Add("OnExit-Active");
    private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
    private void HandleUpdate() => EventLog.Add("InternalAction");
}