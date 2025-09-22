using System.Collections.Generic;
using Abstractions.Fluent;
using static FastFsm.Tests.Features.Core.StateCallbackTests;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(SelfState), typeof(SelfTrigger))]
public partial class SelfTransitionMachineFluent
{
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State(SelfState.Active)
        .OnEntry(nameof(OnEntryActive)).OnExit(nameof(OnExitActive))
        .On(SelfTrigger.Refresh).Action(nameof(RefreshAction)).GoTo(SelfState.Active);

    private void OnEntryActive() => EventLog.Add("OnEntry-Active");
    private void OnExitActive() => EventLog.Add("OnExit-Active");
    private void RefreshAction() => EventLog.Add("RefreshAction");
}