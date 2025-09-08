using Abstractions.Attributes;
using FastFsm.Tests.Features.EdgeCases;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(EmptyMachineTests.InternalOnlyState), typeof(EmptyMachineTests.InternalOnlyTrigger))]
    public partial class InternalOnlyMachineLegacy
    {
        private int _actionCount;
        public int ActionCount => _actionCount;

        [InternalTransition(EmptyMachineTests.InternalOnlyState.Static, EmptyMachineTests.InternalOnlyTrigger.Action, Action = nameof(PerformAction))]
        private void ConfigureTransitions() { }

        private void PerformAction() => _actionCount++;
    }
}