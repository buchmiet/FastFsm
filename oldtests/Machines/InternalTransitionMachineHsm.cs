using Abstractions.Attributes;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class InternalTransitionMachineHsm
{
    // Parent with children
    [State(HsmState.InternalParent,
        OnEntry = nameof(OnParentEntry))]
    private void ConfigureInternalParent() { }

    [State(HsmState.InternalParent_Child1,
        Parent = HsmState.InternalParent,
        IsInitial = true,
        OnEntry = nameof(OnChild1Entry),
        OnExit = nameof(OnChild1Exit))]
    private void ConfigureInternalChild1() { }

    [State(HsmState.InternalParent_Child2,
        Parent = HsmState.InternalParent,
        OnEntry = nameof(OnChild2Entry))]
    private void ConfigureInternalChild2() { }

    // Internal transitions (no state change)
    [InternalTransition(HsmState.InternalParent, HsmTrigger.InternalUpdate,
        Action = nameof(ParentInternalAction))]
    [InternalTransition(HsmState.InternalParent_Child1, HsmTrigger.InternalProcess,
        Guard = nameof(CanProcessInternal),
        Action = nameof(Child1InternalAction))]
    [InternalTransition(HsmState.InternalParent_Child2, HsmTrigger.InternalUpdate,
        Priority = 100,
        Action = nameof(Child2InternalAction))]
    private void ConfigureInternalTransitions() { }

    // Regular transition for comparison
    [Transition(HsmState.InternalParent_Child1, HsmTrigger.MoveNext, HsmState.InternalParent_Child2,
        Action = nameof(RegularTransitionAction))]
    private void ConfigureRegularTransition() { }

    // Callback methods
    private void OnParentEntry() { }
    private void OnChild1Entry() { }
    private void OnChild1Exit() { }
    private void OnChild2Entry() { }
    private void ParentInternalAction() { }
    private void Child1InternalAction() { }
    private void Child2InternalAction() { }
    private void RegularTransitionAction() { }
    private bool CanProcessInternal() => true;
}