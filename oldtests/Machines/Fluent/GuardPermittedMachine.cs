using Abstractions.Fluent;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(State), typeof(Trigger))]
public partial class GuardPermittedMachineFluent
{
    public bool Allow { get; set; }

    private bool CanRun() => Allow;

    private void Configure() => FSM
        .State(State.Initial)
        .On(Trigger.Next)
        .Guard(nameof(CanRun))
        .GoTo(State.Final);
}
