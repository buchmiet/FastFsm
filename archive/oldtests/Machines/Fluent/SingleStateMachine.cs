using Abstractions.Fluent;

namespace FastFsm.Tests.Machines.Fluent;

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