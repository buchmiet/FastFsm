namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(GuardPermittedState), typeof(GuardPermittedTrigger))]
public partial class GuardPermittedMachine
{
    public bool Allow { get; set; }

    private bool CanRun() => Allow;

    [Transition(GuardPermittedState.Idle, GuardPermittedTrigger.Run, GuardPermittedState.Done,
        Guard = (CanRun))]
    private void Configure() { }
}