using Abstractions.Attributes;
using static StateMachine.Tests.EdgeCases.EmptyMachineTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(InternalOnlyState), typeof(InternalOnlyTrigger))]
    public partial class InternalOnlyMachine
    {
        private int _actionCount;
        public int ActionCount => _actionCount;

        [InternalTransition(InternalOnlyState.Static, InternalOnlyTrigger.Action,
            nameof(PerformAction))]
        private void Configure() { }

        private void PerformAction() => _actionCount++;
    }
}
