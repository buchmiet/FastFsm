using Abstractions.Attributes;


namespace StateMachine.Tests.EdgeCases
{
    [StateMachine(typeof(EmptyMachineTests.EmptyState), typeof(EmptyMachineTests.EmptyTrigger))]
    public partial class NoTransitionsMachine
    {
        // No transitions defined
        private void NoConfig() { }
    }
}
