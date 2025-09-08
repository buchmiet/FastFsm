using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(UnreachableState), typeof(UnreachableTrigger))]
    public partial class UnreachableMachineLegacy
    {
        // Note: No transition TO Isolated state - it's unreachable
        [Transition(UnreachableState.Start, UnreachableTrigger.Connect, UnreachableState.Connected)]
        [Transition(UnreachableState.Connected, UnreachableTrigger.Disconnect, UnreachableState.Start)]
        private void ConfigureTransitions() { }
        
        // Define the Isolated state exists (even though unreachable)
        [State(UnreachableState.Isolated)]
        private void DefineIsolatedState() { }
    }
}