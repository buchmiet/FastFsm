using Abstractions.Attributes;
namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class EdgeCaseMachine
{
    // Single parent with single child
    [State(HsmState.EdgeParent)]
    private void ConfigureEdgeParent() { }

    [State(HsmState.EdgeParent_Child,
        Parent = HsmState.EdgeParent,
        IsInitial = true)]
    private void ConfigureEdgeChild() { }

    // State can be both a child and have History (if it has its own children)
    [State(HsmState.EdgeComplexParent,
        History = HistoryMode.Deep)]
    private void ConfigureComplexWithHistory() { }

    [State(HsmState.EdgeComplexParent_Child1,
        Parent = HsmState.EdgeComplexParent,
        IsInitial = true)]
    private void ConfigureComplexChild() { }

    // Maximum use of attributes on a single state
    [State(HsmState.EdgeComplexParent_Child2,
        Parent = HsmState.EdgeComplexParent,
        OnEntry = nameof(OnMaxEntry),
        OnExit = nameof(OnMaxExit))]
    private void ConfigureMaxAttributes() { }

    // Callback methods
    private void OnMaxEntry() { }
    private void OnMaxExit() { }
}
