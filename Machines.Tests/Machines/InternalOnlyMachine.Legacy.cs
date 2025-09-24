using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(InternalOnlyState), typeof(InternalOnlyTrigger))]
public partial class InternalOnlyMachineLegacy
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    [InternalTransition(InternalOnlyState.Static, InternalOnlyTrigger.Action, Action = nameof(PerformAction))]
    private void ConfigureTransitions() { }

    private void PerformAction() => _actionCount++;
}