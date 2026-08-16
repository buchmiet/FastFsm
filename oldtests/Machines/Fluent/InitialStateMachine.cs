using System.Collections.Generic;
using Abstractions.Fluent;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(InitialState), typeof(InitialTrigger))]
public partial class InitialStateMachine
{
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State<InitialState>(InitialState.Start)
        .OnEntry(nameof(OnEntryStart))
        .OnExit(nameof(OnExitStart))
        .On(InitialTrigger.Go).GoTo(InitialState.Next)
        .State(InitialState.Next)
        .OnEntry(nameof(OnEntryNext));

    private void OnEntryStart() => EventLog.Add("OnEntry-Start");
    private void OnExitStart() => EventLog.Add("OnExit-Start");
    private void OnEntryNext() => EventLog.Add("OnEntry-Next");
}