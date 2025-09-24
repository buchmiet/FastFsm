using System.Collections.Generic;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(SelfState), typeof(SelfTrigger))]
public partial class SelfTransitionMachineLegacy
{
    public List<string> EventLog { get; } = [];

    [State(SelfState.Active, OnEntry = nameof(OnEntryActive), OnExit = nameof(OnExitActive))]
    private void ConfigureStates() { }

    [Transition(SelfState.Active, SelfTrigger.Refresh, SelfState.Active, Action = nameof(RefreshAction))]
    private void ConfigureTransitions() { }

    private void OnEntryActive() => EventLog.Add("OnEntry-Active");
    private void OnExitActive() => EventLog.Add("OnExit-Active");
    private void RefreshAction() => EventLog.Add("RefreshAction");
}