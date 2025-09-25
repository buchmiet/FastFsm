using Abstractions.Fluent;
using Machines.Tests.Features.EdgeCases;

namespace Machines.Tests.Machines;

[StateMachine(typeof(InternalOnlyState), typeof(InternalOnlyTrigger))]
public partial class InternalOnlyMachineFluent
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    private void Configure() => FSM
        .State(InternalOnlyState.Static)
        .OnInternal(InternalOnlyTrigger.Action).Action(nameof(PerformAction));

    private void PerformAction() => _actionCount++;
}