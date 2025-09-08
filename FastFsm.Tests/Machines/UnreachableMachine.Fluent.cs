using Abstractions.Attributes;
using Abstractions.Fluent;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(UnreachableState), typeof(UnreachableTrigger))]
    public partial class UnreachableMachineFluent
    {
        // Note: No transition TO Isolated state - it's unreachable
        private static void Configure() => FSM
            .State(UnreachableState.Start)
                .On(UnreachableTrigger.Connect).GoTo(UnreachableState.Connected)
            .State(UnreachableState.Connected)
                .On(UnreachableTrigger.Disconnect).GoTo(UnreachableState.Start)
            .State(UnreachableState.Isolated);
    }
}
