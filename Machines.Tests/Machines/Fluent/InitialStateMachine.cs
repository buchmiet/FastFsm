using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(InitialState), typeof(InitialTrigger))]
public partial class InitialStateMachineFluent
{
    public List<string> EventLog { get; } = [];

    private void Configure() => FSM
        .State<InitialState>(InitialState.Start)
        .OnEntry((OnEntryStart))
        .OnExit((OnExitStart))
        .On(InitialTrigger.Go).GoTo(InitialState.Next)
        .State(InitialState.Next)
        .OnEntry((OnEntryNext));

    private void OnEntryStart() => EventLog.Add("OnEntry-Start");
    private void OnExitStart() => EventLog.Add("OnExit-Start");
    private void OnEntryNext() => EventLog.Add("OnEntry-Next");
}