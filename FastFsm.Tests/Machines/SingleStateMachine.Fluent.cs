using Abstractions.Fluent;
using static FastFsm.Tests.Features.EdgeCases.EmptyMachineTests;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(SingleState), typeof(SingleTrigger))]
public partial class SingleStateMachineFluent
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    private void Configure() => FSM
        .State(SingleState.Only)
        .On(SingleTrigger.Loop).Action(nameof(IncrementCounter)).GoTo(SingleState.Only);

    private void IncrementCounter() => _actionCount++;
}