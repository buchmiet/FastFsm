using Abstractions.Attributes;
namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class ComplexMixedScenarioMachine
{
    // Complex parent with multiple children
    [State(HsmState.ComplexParent,
        OnEntry = nameof(OnComplexParentEntry),
        History = HistoryMode.Shallow)]
    private void ConfigureComplexParent() { }

    [State(HsmState.ComplexParent_Child1,
        Parent = HsmState.ComplexParent,
        IsInitial = true,
        OnEntry = nameof(OnChild1Entry))]
    private void ConfigureComplexChild1() { }

    [State(HsmState.ComplexParent_Child2,
        Parent = HsmState.ComplexParent,
        OnEntry = nameof(OnChild2Entry),
        OnExit = nameof(OnChild2Exit))]
    private void ConfigureComplexChild2() { }

    [State(HsmState.ComplexParent_Child3,
        Parent = HsmState.ComplexParent)]
    private void ConfigureComplexChild3() { }

    // Mixed transitions with guards, actions, and priorities
    [Transition(HsmState.ComplexParent_Child1, HsmTrigger.Process, HsmState.ComplexParent_Child2,
        Priority = 500,
        Guard = nameof(CanTransition),
        Action = nameof(TransitionAction))]
    [Transition(HsmState.ComplexParent_Child2, HsmTrigger.Process, HsmState.ComplexParent_Child3,
        Priority = 100)]
    [InternalTransition(HsmState.ComplexParent_Child1, HsmTrigger.InternalUpdate,
        Priority = 1000,
        Action = nameof(InternalAction))]
    private void ConfigureComplexTransitions() { }

    // Callback methods
    private void OnComplexParentEntry() { }
    private void OnChild1Entry() { }
    private void OnChild2Entry() { }
    private void OnChild2Exit() { }
    private bool CanTransition() => true;
    private void TransitionAction() { }
    private void InternalAction() { }
}
