using Abstractions.Fluent;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(SingleState), typeof(SingleTrigger))]
public partial class SingleStateMachine
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    private void Configure() => FSM
        .State(SingleState.Only)
        .On(SingleTrigger.Loop).Action(nameof(IncrementCounter)).GoTo(SingleState.Only);

    private void IncrementCounter() => _actionCount++;
}