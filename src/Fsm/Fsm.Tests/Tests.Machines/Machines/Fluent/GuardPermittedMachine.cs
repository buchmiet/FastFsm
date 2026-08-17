using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(BasicState), typeof(Trigger))]
public partial class GuardPermittedMachine
{
    public bool Allow { get; set; }

    private bool CanRun() => Allow;

    private void Configure() => FSM
        .State(BasicState.Initial)
        .On(Trigger.Next)
        .Guard((CanRun))
        .GoTo(BasicState.Final);
}
