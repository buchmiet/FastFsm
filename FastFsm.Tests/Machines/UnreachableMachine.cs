using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
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
