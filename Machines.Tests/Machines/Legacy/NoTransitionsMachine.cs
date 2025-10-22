namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachine
{
    // No transitions defined
    private void NoConfig() { }
}