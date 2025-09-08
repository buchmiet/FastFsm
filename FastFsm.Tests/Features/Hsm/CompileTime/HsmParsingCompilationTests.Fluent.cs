using Abstractions.Attributes;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.CompileTime
{
    // Shared enums for HSM tests
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
        
        // Deep history states - with nested children
        DeepHistoryParent,
        DeepHistoryParent_Child1,
        DeepHistoryParent_Child1_SubChild1,
        DeepHistoryParent_Child1_SubChild2,
        DeepHistoryParent_Child2,
        
        // Priority states
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
        
        // Complex parent for mixed scenarios
        ComplexParent,
        ComplexParent_Child1,
        ComplexParent_Child2,
        ComplexParent_Child3,
        
        // Edge case states
        EdgeParent,
        EdgeParent_Child,
        EdgeComplexParent,
        EdgeComplexParent_Child1,
        EdgeComplexParent_Child2,
        
        // Initial state test states
        InitialParent,
        InitialParent_FirstChild,
        InitialParent_SecondChild
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
    #region Fluent API Versions

    #region 1. DeepHierarchyMachineFluent

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class DeepHierarchyMachineFluent
    {
        private void SetupStates()
        {
            // Level 1
            FSM.State(HsmState.Working);
            
            // Level 2  
            FSM.State(HsmState.Working_Processing)
                .ChildOf(HsmState.Working)
                .Initial(HsmState.Working_Processing_Computing);
            
            // Level 3
            FSM.State(HsmState.Working_Processing_Computing)
                .ChildOf(HsmState.Working_Processing)
                .Initial(HsmState.Working_Processing_Computing_Loading);
            
            // Level 4
            FSM.State(HsmState.Working_Processing_Computing_Loading)
                .ChildOf(HsmState.Working_Processing_Computing)
                .OnEntry(nameof(OnLoadingEntry));
                
            FSM.State(HsmState.Working_Processing_Computing_Calculating)
                .ChildOf(HsmState.Working_Processing_Computing)
                .OnEntry(nameof(OnCalculatingEntry))
                .OnExit(nameof(OnCalculatingExit));
                
            FSM.State(HsmState.Working_Processing_Computing_Storing)
                .ChildOf(HsmState.Working_Processing_Computing);
            
            // Cross-level transitions
            FSM.At(HsmState.Working_Processing_Computing_Loading)
                .On(HsmTrigger.Process)
                .GoTo(HsmState.Working_Processing_Computing_Calculating);
                
            FSM.At(HsmState.Working_Processing_Computing_Calculating)
                .On(HsmTrigger.Complete)
                .GoTo(HsmState.Working_Processing_Computing_Storing);
                
            FSM.At(HsmState.Working_Processing_Computing_Storing)
                .On(HsmTrigger.Finish)
                .GoTo(HsmState.Completed);
                
            FSM.At(HsmState.Working)
                .On(HsmTrigger.Abort)
                .GoTo(HsmState.Error);
        }
        
        // Callback methods
        private void OnLoadingEntry() { }
        private void OnCalculatingEntry() { }
        private void OnCalculatingExit() { }
    }

    #endregion

    #region 2. PriorityTransitionMachineFluent

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class PriorityTransitionMachineFluent
    {
        private void SetupStates()
        {
            FSM.State(HsmState.Priority_Low);
            FSM.State(HsmState.Priority_Medium);
            FSM.State(HsmState.Priority_High);
            
            // Multiple transitions from same state with different priorities
            FSM.At(HsmState.Priority_Low)
                .On(HsmTrigger.Execute)
                .GoTo(HsmState.Priority_Medium)
                .Priority(10);
                
            FSM.At(HsmState.Priority_Low)
                .On(HsmTrigger.Execute)
                .GoTo(HsmState.Priority_High)
                .Guard(nameof(HighPriorityGuard))
                .Priority(100);
                
            // Priority in parent-child transitions
            FSM.State(HsmState.ComplexParent);
            
            FSM.State(HsmState.ComplexParent_Child1)
                .ChildOf(HsmState.ComplexParent)
                .OnEntry(nameof(OnChild1Entry));
                
            FSM.State(HsmState.ComplexParent_Child2)
                .ChildOf(HsmState.ComplexParent);
                
            FSM.At(HsmState.ComplexParent)
                .On(HsmTrigger.Process)
                .GoTo(HsmState.ComplexParent_Child1)
                .Priority(50);
                
            FSM.At(HsmState.ComplexParent)
                .On(HsmTrigger.Process)
                .GoTo(HsmState.ComplexParent_Child2)
                .Guard(nameof(SpecialConditionGuard))
                .Priority(200);
        }
        
        // Guards and callbacks
        private bool HighPriorityGuard() => true;
        private bool SpecialConditionGuard() => false;
        private void OnChild1Entry() { }
    }

    #endregion

    #region 3. CrossHierarchyMachineFluent

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class CrossHierarchyMachineFluent
    {
        private void SetupStates()
        {
            // Branch 1
            FSM.State(HsmState.Branch1);
            
            FSM.State(HsmState.Branch1_Leaf1)
                .ChildOf(HsmState.Branch1)
                .OnEntry(nameof(OnLeaf1Entry));
                
            FSM.State(HsmState.Branch1_Leaf2)
                .ChildOf(HsmState.Branch1);
            
            // Branch 2
            FSM.State(HsmState.Branch2);
            
            FSM.State(HsmState.Branch2_Leaf1)
                .ChildOf(HsmState.Branch2)
                .OnEntry(nameof(OnBranch2Leaf1Entry));
                
            FSM.State(HsmState.Branch2_Leaf2)
                .ChildOf(HsmState.Branch2);
            
            // Cross-branch transitions
            FSM.At(HsmState.Branch1_Leaf1)
                .On(HsmTrigger.CrossBranch)
                .GoTo(HsmState.Branch2_Leaf1);
                
            FSM.At(HsmState.Branch2_Leaf2)
                .On(HsmTrigger.CrossBranch)
                .GoTo(HsmState.Branch1_Leaf2);
                
            // Transition from child to different parent
            FSM.At(HsmState.Branch1_Leaf2)
                .On(HsmTrigger.MoveNext)
                .GoTo(HsmState.Branch2);
                
            // Transition from parent to child in different branch
            FSM.At(HsmState.Branch1)
                .On(HsmTrigger.Skip)
                .GoTo(HsmState.Branch2_Leaf1);
        }
        
        // Callbacks
        private void OnLeaf1Entry() { }
        private void OnBranch2Leaf1Entry() { }
    }

    #endregion

    #region 4. ComplexMixedScenarioMachineFluent

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class ComplexMixedScenarioMachineFluent
    {
        private void SetupStates()
        {
            // Parent with history
            FSM.State(HsmState.ComplexParent)
                .HistoryShallow()
                .OnEntry(nameof(OnComplexParentEntry))
                .OnExit(nameof(OnComplexParentExit));
            
            // Children with various configurations
            FSM.State(HsmState.ComplexParent_Child1)
                .ChildOf(HsmState.ComplexParent)
                .OnEntry(nameof(OnChild1Entry))
                .OnExit(nameof(OnChild1Exit));
                
            FSM.State(HsmState.ComplexParent_Child2)
                .ChildOf(HsmState.ComplexParent);
                
            FSM.State(HsmState.ComplexParent_Child3)
                .ChildOf(HsmState.ComplexParent)
                .OnEntry(nameof(OnChild3Entry));
            
            // Internal transitions
            FSM.At(HsmState.ComplexParent)
                .OnInternal(HsmTrigger.InternalUpdate)
                .Action(nameof(ProcessInternalUpdate));
                
            FSM.At(HsmState.ComplexParent_Child1)
                .OnInternal(HsmTrigger.InternalProcess)
                .Action(nameof(ProcessInChild));
            
            // Regular transitions with priority
            FSM.At(HsmState.ComplexParent_Child1)
                .On(HsmTrigger.MoveNext)
                .GoTo(HsmState.ComplexParent_Child2)
                .Priority(10);
                
            FSM.At(HsmState.ComplexParent_Child1)
                .On(HsmTrigger.MoveNext)
                .GoTo(HsmState.ComplexParent_Child3)
                .Guard(nameof(CanSkipToChild3))
                .Priority(100);
                
            FSM.At(HsmState.ComplexParent_Child2)
                .On(HsmTrigger.MoveNext)
                .GoTo(HsmState.ComplexParent_Child3);
                
            FSM.At(HsmState.ComplexParent_Child3)
                .On(HsmTrigger.MovePrevious)
                .GoTo(HsmState.ComplexParent_Child1);
            
            // Exit from hierarchy
            FSM.At(HsmState.ComplexParent)
                .On(HsmTrigger.Complete)
                .GoTo(HsmState.Completed);
                
            FSM.At(HsmState.ComplexParent)
                .On(HsmTrigger.Cancel)
                .GoTo(HsmState.Idle);
        }
        
        // Callbacks
        private void OnComplexParentEntry() { }
        private void OnComplexParentExit() { }
        private void OnChild1Entry() { }
        private void OnChild1Exit() { }
        private void OnChild3Entry() { }
        private void ProcessInternalUpdate() { }
        private void ProcessInChild() { }
        private bool CanSkipToChild3() => true;
    }

    #endregion

    #region 5. EdgeCaseMachineFluent

    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class EdgeCaseMachineFluent
    {
        private void SetupStates()
        {
            // Parent without initial (but has no substates yet, so valid)
            FSM.State(HsmState.EdgeParent)
                .OnEntry(nameof(OnEdgeParentEntry));
            
            // Adding child later
            FSM.State(HsmState.EdgeParent_Child)
                .ChildOf(HsmState.EdgeParent);
            
            // Parent-to-self transition  
            FSM.At(HsmState.EdgeParent)
                .On(HsmTrigger.Reset)
                .GoTo(HsmState.EdgeParent);
            
            // Child-to-parent transition
            FSM.At(HsmState.EdgeParent_Child)
                .On(HsmTrigger.Complete)
                .GoTo(HsmState.EdgeParent);
            
            // Additional complex parent for edge cases
            FSM.State(HsmState.EdgeComplexParent);
            
            FSM.State(HsmState.EdgeComplexParent_Child1)
                .ChildOf(HsmState.EdgeComplexParent);
                
            // Maximum use of attributes on a single state
            FSM.State(HsmState.EdgeComplexParent_Child2)
                .ChildOf(HsmState.EdgeComplexParent)
                .OnEntry(nameof(OnMaxEntry))
                .OnExit(nameof(OnMaxExit));
        }
        
        // Callback methods
        private void OnEdgeParentEntry() { }
        private void OnMaxEntry() { }
        private void OnMaxExit() { }
    }

    #endregion

    #endregion
}