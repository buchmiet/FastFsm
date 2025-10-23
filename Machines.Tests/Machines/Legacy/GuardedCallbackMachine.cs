using Abstractions.Attributes;
﻿namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(GuardedState), typeof(GuardedTrigger))]
public partial class GuardedCallbackMachine
{
    public bool AllowTransition { get; set; }
    public List<string> EventLog { get; } = [];

    [State(GuardedState.A, OnEntry = nameof(OnEntryA), OnExit = nameof(OnExitA))]
    [State(GuardedState.B, OnEntry = nameof(OnEntryB))]
    private void ConfigureStates() { }

    [Transition(GuardedState.A, GuardedTrigger.Go, GuardedState.B,
        Guard = nameof(CanTransition))]
    private void Configure() { }

    private bool CanTransition() => AllowTransition;
    private void OnEntryA() => EventLog.Add("OnEntry-A");
    private void OnExitA() => EventLog.Add("OnExit-A");
    private void OnEntryB() => EventLog.Add("OnEntry-B");
}
