using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(SingleState), typeof(SingleTrigger))]
    public partial class SingleStateMachineLegacy
    {
        private int _actionCount;
        public int ActionCount => _actionCount;

        [Transition(SingleState.Only, SingleTrigger.Loop, SingleState.Only, Action = nameof(IncrementCounter))]
        private void ConfigureTransitions() { }

        private void IncrementCounter() => _actionCount++;
    }
}