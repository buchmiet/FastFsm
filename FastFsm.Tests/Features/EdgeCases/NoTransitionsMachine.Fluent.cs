using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Tests.Features.EdgeCases
{
    [StateMachine(typeof(EmptyMachineTests.EmptyState), typeof(EmptyMachineTests.EmptyTrigger))]
    public partial class NoTransitionsMachineFluent
    {
        // No transitions defined
        private static void Configure() => FSM
            .State<EmptyMachineTests.EmptyState>(EmptyMachineTests.EmptyState.Only);
            // No transitions - this is intentional for testing
    }
}