using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(InternalOnlyState), typeof(InternalOnlyTrigger))]
public partial class InternalOnlyMachine
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    private void Configure() => FSM
        .State(InternalOnlyState.Static)
        .OnInternal(InternalOnlyTrigger.Action).Action((PerformAction));

    private void PerformAction() => _actionCount++;
}