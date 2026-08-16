using FastFsm.Tests.Features.Core;
using Abstractions.Attributes;

namespace FastFsm.Tests.Machines.Legacy;

[StateMachine(typeof(GuardPermittedState), typeof(GuardPermittedTrigger))]
public partial class GuardPermittedMachine
{
    public bool Allow { get; set; }

    private bool CanRun() => Allow;

    [Transition(GuardPermittedState.Idle, GuardPermittedTrigger.Run, GuardPermittedState.Done,
        Guard = nameof(CanRun))]
    private void Configure() { }
}