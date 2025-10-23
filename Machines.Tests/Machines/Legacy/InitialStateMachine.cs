using Abstractions.Attributes;
﻿namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(InitialState), typeof(InitialTrigger))]
public partial class InitialStateMachine
{
    public List<string> EventLog { get; } = [];

    [State(InitialState.Start, OnEntry = (OnEntryStart), OnExit = (OnExitStart))]
    [State(InitialState.Next, OnEntry = (OnEntryNext))]
    private void ConfigureStates() { }

    [Transition(InitialState.Start, InitialTrigger.Go, InitialState.Next)]
    private void Configure() { }

    private void OnEntryStart() => EventLog.Add("OnEntry-Start");
    private void OnExitStart() => EventLog.Add("OnExit-Start");
    private void OnEntryNext() => EventLog.Add("OnEntry-Next");
}
