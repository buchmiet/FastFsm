using Abstractions.Fluent;
using Machines.Tests.Features.EdgeCases;

namespace Machines.Tests.Machines;

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