using Abstractions.Attributes;
using static StateMachine.Tests.EdgeCases.EmptyMachineTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(UnreachableState), typeof(UnreachableTrigger))]
    public partial class UnreachableMachine
    {
        [Transition(UnreachableState.Start, UnreachableTrigger.Connect, UnreachableState.Connected)]
        [Transition(UnreachableState.Connected, UnreachableTrigger.Disconnect, UnreachableState.Start)]
        // Note: No transition TO Isolated state - it's unreachable
        private void Configure() { }
    }
}
