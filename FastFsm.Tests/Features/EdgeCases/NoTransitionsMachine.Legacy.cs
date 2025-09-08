using Abstractions.Attributes;


namespace FastFsm.Tests.Features.EdgeCases
{
    [StateMachine(typeof(EmptyMachineTests.EmptyState), typeof(EmptyMachineTests.EmptyTrigger))]
    public partial class NoTransitionsMachineLegacy
    {
        // No transitions defined
        private void NoConfig() { }
    }
}
