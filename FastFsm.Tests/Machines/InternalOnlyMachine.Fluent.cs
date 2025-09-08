using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.EdgeCases;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(EmptyMachineTests.InternalOnlyState), typeof(EmptyMachineTests.InternalOnlyTrigger))]
    public partial class InternalOnlyMachineFluent
    {
        private int _actionCount;
        public int ActionCount => _actionCount;

        private static void Configure() => FSM
            .State(EmptyMachineTests.InternalOnlyState.Static)
                .OnInternal(EmptyMachineTests.InternalOnlyTrigger.Action).Action(nameof(PerformAction));

        private void PerformAction() => _actionCount++;
    }
}
