using System.Collections.Generic;
using Abstractions.Fluent;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(InternalState), typeof(InternalTrigger))]
public partial class InternalTransitionMachineFluent
{
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State(InternalState.Active)
        .OnEntry(nameof(OnEntryActive)).OnExit(nameof(OnExitActive))
        .On(InternalTrigger.Deactivate).GoTo(InternalState.Inactive)
        .State(InternalState.Inactive)
        .OnEntry(nameof(OnEntryInactive))
        .State(InternalState.Active)
        .OnInternal(InternalTrigger.Update).Action(nameof(HandleUpdate));

    private void OnEntryActive() => EventLog.Add("OnEntry-Active");
    private void OnExitActive() => EventLog.Add("OnExit-Active");
    private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
    private void HandleUpdate() => EventLog.Add("InternalAction");
}