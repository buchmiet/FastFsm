using System.Collections.Generic;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(InternalState), typeof(InternalTrigger))]
public partial class InternalTransitionMachineLegacy
{
    public List<string> EventLog { get; } = [];

    [State(InternalState.Active, OnEntry = nameof(OnEntryActive), OnExit = nameof(OnExitActive))]
    [State(InternalState.Inactive, OnEntry = nameof(OnEntryInactive))]
    private void ConfigureStates() { }

    [Transition(InternalState.Active, InternalTrigger.Deactivate, InternalState.Inactive)]
    private void TransitionDeactivate() { }

    [InternalTransition(InternalState.Active, InternalTrigger.Update, Action = nameof(HandleUpdate))]
    private void InternalUpdate() { }

    private void OnEntryActive() => EventLog.Add("OnEntry-Active");
    private void OnExitActive() => EventLog.Add("OnExit-Active");
    private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
    private void HandleUpdate() => EventLog.Add("InternalAction");
}