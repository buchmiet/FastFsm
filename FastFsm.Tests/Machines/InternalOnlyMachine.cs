using Abstractions.Attributes;
using FastFsm.Tests.Features.EdgeCases;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(EmptyMachineTests.InternalOnlyState), typeof(EmptyMachineTests.InternalOnlyTrigger))]
    public partial class InternalOnlyMachine
    {
        private int _actionCount;
        public int ActionCount => _actionCount;

        [InternalTransition(EmptyMachineTests.InternalOnlyState.Static, EmptyMachineTests.InternalOnlyTrigger.Action,
            Action = nameof(PerformAction))]
        private void Configure() { }

        private void PerformAction() => _actionCount++;
    }
}
