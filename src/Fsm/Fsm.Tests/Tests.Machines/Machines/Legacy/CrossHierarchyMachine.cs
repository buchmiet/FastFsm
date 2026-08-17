using Abstractions.Attributes;
namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class CrossHierarchyMachine
{
    // Branch 1
    [State(HsmState.Branch1)]
    private void ConfigureBranch1() { }

    [State(HsmState.Branch1_Leaf1,
        Parent = HsmState.Branch1,
        IsInitial = true)]
    private void ConfigureBranch1Leaf1() { }

    [State(HsmState.Branch1_Leaf2,
        Parent = HsmState.Branch1)]
    private void ConfigureBranch1Leaf2() { }

    // Branch 2
    [State(HsmState.Branch2)]
    private void ConfigureBranch2() { }

    [State(HsmState.Branch2_Leaf1,
        Parent = HsmState.Branch2,
        IsInitial = true)]
    private void ConfigureBranch2Leaf1() { }

    [State(HsmState.Branch2_Leaf2,
        Parent = HsmState.Branch2)]
    private void ConfigureBranch2Leaf2() { }

    // Cross-branch transitions
    [Transition(HsmState.Branch1_Leaf1, HsmTrigger.CrossBranch, HsmState.Branch2_Leaf2)]
    [Transition(HsmState.Branch2_Leaf1, HsmTrigger.CrossBranch, HsmState.Branch1_Leaf2)]
    [Transition(HsmState.Branch1, HsmTrigger.CrossBranch, HsmState.Branch2)]
    private void ConfigureCrossTransitions() { }
}
