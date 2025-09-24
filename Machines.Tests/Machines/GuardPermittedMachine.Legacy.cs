using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(State), typeof(Trigger))]
public partial class GuardPermittedMachineLegacy
{
    public bool Allow { get; set; }

    private bool CanRun() => Allow;

    [Transition(State.Idle, Trigger.Run, State.Done, Guard = nameof(CanRun))]
    private void Configure() { }
}
