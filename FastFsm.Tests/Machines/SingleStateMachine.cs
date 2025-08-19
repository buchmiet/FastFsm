using Abstractions.Attributes;
using static StateMachine.Tests.Features.EdgeCases.EmptyMachineTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(SingleState), typeof(SingleTrigger))]
    public partial class SingleStateMachine
    {
        private int _actionCount;
        public int ActionCount => _actionCount;

        [Transition(SingleState.Only, SingleTrigger.Loop, SingleState.Only,
            Action = nameof(IncrementCounter))]
        private void Configure() { }

        private void IncrementCounter() => _actionCount++;
    }
}
