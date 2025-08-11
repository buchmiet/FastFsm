using Xunit;
using Abstractions.Attributes;

namespace StateMachine.Tests.HierarchicalTests;

/// <summary>
/// HSM Parsing Compilation Tests
/// This file contains ONLY VALID HSM configurations that should compile successfully.
/// All machines here represent correct usage of HSM attributes.
/// Invalid/error cases should be tested in a separate diagnostics project.
/// </summary>
public partial class HsmParsingCompilationTests
{
    #region Test Enums

    public enum HsmState
    {
        // Root states
        Idle,
        Working,
        Completed,
        Error,
        Paused,
        
        // Working substates (2nd level)
        Working_Initializing,
        Working_Processing,
        Working_Validating,
        Working_Cleanup,
        
        // Working_Processing substates (3rd level)
        Working_Processing_Reading,
        Working_Processing_Computing,
        Working_Processing_Writing,
        
        // Working_Processing_Computing substates (4th level - deep hierarchy)
        Working_Processing_Computing_Loading,
        Working_Processing_Computing_Calculating,
        Working_Processing_Computing_Storing,
        
        // History test states - with proper children
        HistoryParent,
        HistoryParent_Child1,
        HistoryParent_Child2,
        HistoryParent_Child3,
        
        // Deep history parent with nested children
        DeepHistoryParent,
        DeepHistoryParent_Child1,
        DeepHistoryParent_Child1_SubChild1,
        DeepHistoryParent_Child1_SubChild2,
        DeepHistoryParent_Child2,
        
        // Priority test states
        Priority_Low,
        Priority_Medium,
        Priority_High,
        
        // Internal transition test states
        InternalParent,
        InternalParent_Child1,
        InternalParent_Child2,
        
        // Cross-hierarchy test states
        Branch1,
        Branch1_Leaf1,
        Branch1_Leaf2,
        Branch2,
        Branch2_Leaf1,
        Branch2_Leaf2,
        
        // Complex scenario states
        ComplexParent,
        ComplexParent_Child1,
        ComplexParent_Child2,
        ComplexParent_Child3,
        
        // Edge case states
        EdgeParent,
        EdgeParent_Child,
        
        // Edge case complex states (unique to avoid conflicts)
        EdgeComplexParent,
        EdgeComplexParent_Child1,
        EdgeComplexParent_Child2
    }

    public enum HsmTrigger
    {
        Start,
        Process,
        Complete,
        Validate,
        Execute,
        Pause,
        Resume,
        Reset,
        Initialize,
        Activate,
        Deactivate,
        Submit,
        Approve,
        Reject,
        Timeout,
        Error,
        Recover,
        InternalUpdate,
        InternalProcess,
        MoveNext,
        MovePrevious,
        CrossBranch,
        Abort,
        Finish,
        Cancel,
        Retry,
        Skip
    }

    #endregion

    #region 1. Simple Parent-Child Hierarchy (VALID)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class SimpleParentChildMachine
    {
        // Parent state with children
        [State(HsmState.Working, 
            OnEntry = nameof(OnWorkingEntry), 
            OnExit = nameof(OnWorkingExit))]
        private void ConfigureWorking() { }
        
        // Child states with proper Parent reference
        [State(HsmState.Working_Initializing, 
            Parent = HsmState.Working, 
            IsInitial = true,
            OnEntry = nameof(OnInitializingEntry),
            OnExit = nameof(OnInitializingExit))]
        private void ConfigureInitializing() { }
        
        [State(HsmState.Working_Processing, 
            Parent = HsmState.Working,
            OnEntry = nameof(OnProcessingEntry))]
        private void ConfigureProcessing() { }
        
        [State(HsmState.Working_Validating, 
            Parent = HsmState.Working)]
        private void ConfigureValidating() { }

        // Valid transitions
        [Transition(HsmState.Idle, HsmTrigger.Start, HsmState.Working)]
        [Transition(HsmState.Working_Initializing, HsmTrigger.Process, HsmState.Working_Processing)]
        [Transition(HsmState.Working_Processing, HsmTrigger.Validate, HsmState.Working_Validating)]
        [Transition(HsmState.Working, HsmTrigger.Complete, HsmState.Completed)]
        private void ConfigureTransitions() { }
        
        // Callback methods
        private void OnWorkingEntry() { }
        private void OnWorkingExit() { }
        private void OnInitializingEntry() { }
        private void OnInitializingExit() { }
        private void OnProcessingEntry() { }
    }

    #endregion

    #region 2. Deep Hierarchy - 4 Levels (VALID)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class DeepHierarchyMachine
    {
        // Level 1
        [State(HsmState.Working)]
        private void ConfigureLevel1() { }
        
        // Level 2
        [State(HsmState.Working_Processing, 
            Parent = HsmState.Working,
            IsInitial = true)]
        private void ConfigureLevel2() { }
        
        // Level 3
        [State(HsmState.Working_Processing_Computing, 
            Parent = HsmState.Working_Processing,
            IsInitial = true)]
        private void ConfigureLevel3() { }
        
        // Level 4
        [State(HsmState.Working_Processing_Computing_Loading, 
            Parent = HsmState.Working_Processing_Computing, 
            IsInitial = true, 
            OnEntry = nameof(OnLoadingEntry))]
        private void ConfigureLoading() { }
        
        [State(HsmState.Working_Processing_Computing_Calculating, 
            Parent = HsmState.Working_Processing_Computing, 
            OnEntry = nameof(OnCalculatingEntry), 
            OnExit = nameof(OnCalculatingExit))]
        private void ConfigureCalculating() { }
        
        [State(HsmState.Working_Processing_Computing_Storing, 
            Parent = HsmState.Working_Processing_Computing)]
        private void ConfigureStoring() { }

        // Cross-level transitions
        [Transition(HsmState.Working_Processing_Computing_Loading, HsmTrigger.Process, HsmState.Working_Processing_Computing_Calculating)]
        [Transition(HsmState.Working_Processing_Computing_Calculating, HsmTrigger.Complete, HsmState.Working_Processing_Computing_Storing)]
        [Transition(HsmState.Working_Processing_Computing_Storing, HsmTrigger.Finish, HsmState.Completed)]
        [Transition(HsmState.Working, HsmTrigger.Abort, HsmState.Error)]
        private void ConfigureDeepTransitions() { }
        
        // Callback methods
        private void OnLoadingEntry() { }
        private void OnCalculatingEntry() { }
        private void OnCalculatingExit() { }
    }

    #endregion

    #region 3. Shallow History (VALID - has children)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class ShallowHistoryMachine
    {
        // Parent with shallow history AND children
        [State(HsmState.HistoryParent, 
            History = HistoryMode.Shallow)]
        private void ConfigureHistoryParent() { }
        
        // Children of HistoryParent
        [State(HsmState.HistoryParent_Child1, 
            Parent = HsmState.HistoryParent, 
            IsInitial = true)]
        private void ConfigureChild1() { }
        
        [State(HsmState.HistoryParent_Child2, 
            Parent = HsmState.HistoryParent)]
        private void ConfigureChild2() { }
        
        [State(HsmState.HistoryParent_Child3, 
            Parent = HsmState.HistoryParent)]
        private void ConfigureChild3() { }

        // Transitions between children
        [Transition(HsmState.HistoryParent_Child1, HsmTrigger.MoveNext, HsmState.HistoryParent_Child2)]
        [Transition(HsmState.HistoryParent_Child2, HsmTrigger.MoveNext, HsmState.HistoryParent_Child3)]
        [Transition(HsmState.HistoryParent_Child3, HsmTrigger.MovePrevious, HsmState.HistoryParent_Child1)]
        private void ConfigureHistoryTransitions() { }
    }

    #endregion

    #region 4. Deep History (VALID - has nested children)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class DeepHistoryMachine
    {
        // Parent with deep history
        [State(HsmState.DeepHistoryParent, 
            History = HistoryMode.Deep)]
        private void ConfigureDeepParent() { }
        
        // First level children
        [State(HsmState.DeepHistoryParent_Child1, 
            Parent = HsmState.DeepHistoryParent, 
            IsInitial = true)]
        private void ConfigureDeepChild1() { }
        
        [State(HsmState.DeepHistoryParent_Child2, 
            Parent = HsmState.DeepHistoryParent)]
        private void ConfigureDeepChild2() { }
        
        // Nested children (grandchildren)
        [State(HsmState.DeepHistoryParent_Child1_SubChild1, 
            Parent = HsmState.DeepHistoryParent_Child1, 
            IsInitial = true)]
        private void ConfigureSubChild1() { }
        
        [State(HsmState.DeepHistoryParent_Child1_SubChild2, 
            Parent = HsmState.DeepHistoryParent_Child1)]
        private void ConfigureSubChild2() { }

        // Transitions
        [Transition(HsmState.DeepHistoryParent_Child1_SubChild1, HsmTrigger.MoveNext, HsmState.DeepHistoryParent_Child1_SubChild2)]
        [Transition(HsmState.DeepHistoryParent_Child1, HsmTrigger.MoveNext, HsmState.DeepHistoryParent_Child2)]
        private void ConfigureDeepHistoryTransitions() { }
    }

    #endregion

    #region 5. Priority Transitions (VALID)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class PriorityTransitionMachine
    {
        // Multiple transitions with different priorities from same state
        [Transition(HsmState.Priority_Low, HsmTrigger.Execute, HsmState.Priority_Medium, 
            Priority = 0, 
            Action = nameof(LowPriorityAction))]
        [Transition(HsmState.Priority_Low, HsmTrigger.Execute, HsmState.Priority_High, 
            Priority = 1000, 
            Guard = nameof(IsHighPriorityNeeded),
            Action = nameof(HighPriorityAction))]
        private void ConfigurePriorityTransitions() { }
        
        // Different priority on different states
        [Transition(HsmState.Priority_Medium, HsmTrigger.Execute, HsmState.Priority_High, 
            Priority = 500, 
            Action = nameof(MediumPriorityAction))]
        [Transition(HsmState.Priority_High, HsmTrigger.Reset, HsmState.Priority_Low, 
            Priority = 100)]
        private void ConfigureOtherPriorities() { }
        
        // Action and guard methods
        private void HighPriorityAction() { }
        private void MediumPriorityAction() { }
        private void LowPriorityAction() { }
        private bool IsHighPriorityNeeded() => true;
    }

    #endregion

    #region 6. Internal Transitions (VALID)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class InternalTransitionMachine
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

    #endregion

    #region 7. Cross-Hierarchy Transitions (VALID)

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

    #endregion

    #region 8. Complex Mixed Scenario (VALID)

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

    #endregion

    #region 9. Initial State Configuration (VALID)

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class InitialStateMachine
    {
        // Parent with explicit initial child
        [State(HsmState.Working)]
        private void ConfigureWorkingParent() { }
        
        // Multiple children, one marked as initial
        [State(HsmState.Working_Initializing, 
            Parent = HsmState.Working, 
            IsInitial = true)]
        private void ConfigureInitial() { }
        
        [State(HsmState.Working_Processing, 
            Parent = HsmState.Working, 
            IsInitial = false)]  // Explicitly not initial
        private void ConfigureNonInitial() { }
        
        [State(HsmState.Working_Validating, 
            Parent = HsmState.Working)]  // Default (not initial)
        private void ConfigureDefault() { }
        
        [State(HsmState.Working_Cleanup, 
            Parent = HsmState.Working)]
        private void ConfigureCleanup() { }
    }

    #endregion

    #region 10. Edge Cases (VALID)

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

    #endregion

    #region Compilation Tests

    [Fact]
    public void AllHsmMachinesShouldCompile()
    {
        // This test passes if all the state machines compile successfully
        // The actual compilation happens at build time
        Assert.True(true, "All HSM parsing tests compiled successfully");
    }

    [Fact]
    public void SimpleParentChildMachineCanBeInstantiated()
    {
        var machine = new SimpleParentChildMachine(HsmState.Idle);
        Assert.NotNull(machine);
        Assert.Equal(HsmState.Idle, machine.CurrentState);
    }

    [Fact]
    public void DeepHierarchyMachineCanBeInstantiated()
    {
        var machine = new DeepHierarchyMachine(HsmState.Working);
        Assert.NotNull(machine);
        Assert.Equal(HsmState.Working, machine.CurrentState);
    }

    [Fact]
    public void ShallowHistoryMachineCanBeInstantiated()
    {
        var machine = new ShallowHistoryMachine(HsmState.HistoryParent);
        Assert.NotNull(machine);
    }

    [Fact]
    public void DeepHistoryMachineCanBeInstantiated()
    {
        var machine = new DeepHistoryMachine(HsmState.DeepHistoryParent);
        Assert.NotNull(machine);
    }

    [Fact]
    public void PriorityTransitionMachineCanBeInstantiated()
    {
        var machine = new PriorityTransitionMachine(HsmState.Priority_Low);
        Assert.NotNull(machine);
    }

    [Fact]
    public void InternalTransitionMachineCanBeInstantiated()
    {
        var machine = new InternalTransitionMachine(HsmState.InternalParent);
        Assert.NotNull(machine);
    }

    [Fact]
    public void CrossHierarchyMachineCanBeInstantiated()
    {
        var machine = new CrossHierarchyMachine(HsmState.Branch1);
        Assert.NotNull(machine);
    }

    [Fact]
    public void ComplexMixedScenarioMachineCanBeInstantiated()
    {
        var machine = new ComplexMixedScenarioMachine(HsmState.ComplexParent);
        Assert.NotNull(machine);
    }

    [Fact]
    public void InitialStateMachineCanBeInstantiated()
    {
        var machine = new InitialStateMachine(HsmState.Working);
        Assert.NotNull(machine);
    }

    [Fact]
    public void EdgeCaseMachineCanBeInstantiated()
    {
        var machine = new EdgeCaseMachine(HsmState.EdgeParent);
        Assert.NotNull(machine);
    }

    #endregion
}