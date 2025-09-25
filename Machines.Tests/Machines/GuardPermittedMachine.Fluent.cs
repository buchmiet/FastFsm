using Abstractions.Fluent;
using Machines.Tests.Features.Core;

namespace Machines.Tests.Machines;

[StateMachine(typeof(State), typeof(Trigger))]
public partial class GuardPermittedMachineFluent
{
    public bool Allow { get; set; }

    private bool CanRun() => Allow;

    private void Configure() => FSM
        .State(State.Idle)
        .On(Trigger.Run)
        .Guard(nameof(CanRun))
        .GoTo(State.Done);
}
