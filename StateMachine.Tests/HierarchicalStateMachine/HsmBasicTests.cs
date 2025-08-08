//using System;
//using System.Linq;
//using Xunit;
//using Xunit.Abstractions;

//namespace StateMachine.Tests.HierarchicalStateMachine;

//public class HsmBasicTests
//{
//    private readonly ITestOutputHelper _output;

//    public HsmBasicTests(ITestOutputHelper output)
//    {
//        _output = output;
//    }

//    [Fact]
//    public void CompositeState_Initial_EntersInitialChild()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.A);

//        // Assert - Should automatically enter A1 (initial child of A)
//        Assert.Equal(HsmState.A1, machine.CurrentState);
//        Assert.Contains("Entry-A", machine.ExecutionLog);
//        Assert.Contains("Entry-A1", machine.ExecutionLog);
        
//        // Verify order
//        var aIndex = machine.ExecutionLog.IndexOf("Entry-A");
//        var a1Index = machine.ExecutionLog.IndexOf("Entry-A1");
//        Assert.True(aIndex < a1Index, "Parent entry should come before child entry");
//    }

//    [Fact]
//    public void TransitionToComposite_EntersInitialChild()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.D);
//        machine.ExecutionLog.Clear();

//        // Act - Transition to composite state A (should enter A1 automatically)
//        machine.Fire(HsmTrigger.ToA);

//        // Assert
//        Assert.Equal(HsmState.A1, machine.CurrentState);
//        Assert.Contains("Exit-D", machine.ExecutionLog);
//        Assert.Contains("Entry-A", machine.ExecutionLog);
//        Assert.Contains("Entry-A1", machine.ExecutionLog);
        
//        // Verify order
//        var exitIndex = machine.ExecutionLog.IndexOf("Exit-D");
//        var entryAIndex = machine.ExecutionLog.IndexOf("Entry-A");
//        var entryA1Index = machine.ExecutionLog.IndexOf("Entry-A1");
//        Assert.True(exitIndex < entryAIndex, "Exit should come before entry");
//        Assert.True(entryAIndex < entryA1Index, "Parent entry should come before child entry");
//    }

//    [Fact]
//    public void ShallowHistory_RestoresLastDirectChild()
//    {
//        // Arrange
//        var machine = new ShallowHistoryMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act - Move to A2
//        machine.Fire(HsmTrigger.Next);
//        Assert.Equal(HsmState.A2, machine.CurrentState);
        
//        // Leave composite A
//        machine.Fire(HsmTrigger.ToB);
//        Assert.Equal(HsmState.B, machine.CurrentState);
        
//        machine.ExecutionLog.Clear();
        
//        // Return to A - should restore A2 (shallow history)
//        machine.Fire(HsmTrigger.ToA);

//        // Assert
//        Assert.Equal(HsmState.A2, machine.CurrentState);
//        Assert.Contains("Entry-A", machine.ExecutionLog);
//        Assert.Contains("Entry-A2", machine.ExecutionLog);
//        Assert.DoesNotContain("Entry-A1", machine.ExecutionLog);
//    }

//    [Fact]
//    public void DeepHistory_RestoresFullPath()
//    {
//        // Arrange
//        var machine = new DeepHistoryMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act - Navigate to nested state A3 (child of A2)
//        machine.Fire(HsmTrigger.Next);
//        Assert.Equal(HsmState.A3, machine.CurrentState);
        
//        // Leave composite A
//        machine.Fire(HsmTrigger.ToB);
//        Assert.Equal(HsmState.B, machine.CurrentState);
        
//        machine.ExecutionLog.Clear();
        
//        // Return to A - should restore full path to A3
//        machine.Fire(HsmTrigger.ToA);

//        // Assert
//        Assert.Equal(HsmState.A3, machine.CurrentState);
//        Assert.Contains("Entry-A", machine.ExecutionLog);
//        Assert.Contains("Entry-A2", machine.ExecutionLog);
//        Assert.Contains("Entry-A3", machine.ExecutionLog);
        
//        // Verify order
//        var entryAIndex = machine.ExecutionLog.IndexOf("Entry-A");
//        var entryA2Index = machine.ExecutionLog.IndexOf("Entry-A2");
//        var entryA3Index = machine.ExecutionLog.IndexOf("Entry-A3");
//        Assert.True(entryAIndex < entryA2Index);
//        Assert.True(entryA2Index < entryA3Index);
//    }

//    [Fact]
//    public void TransitionInheritance_ChildInheritsParentTransitions()
//    {
//        // Arrange
//        var machine = new TransitionInheritanceMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act - Fire trigger defined on parent A
//        machine.Fire(HsmTrigger.ToB);

//        // Assert - Transition should work from child A1
//        Assert.Equal(HsmState.B, machine.CurrentState);
//        Assert.Contains("Action-A-to-B", machine.ExecutionLog);
//    }

//    [Fact]
//    public void TransitionInheritance_ChildOverridesParent()
//    {
//        // Arrange
//        var machine = new TransitionInheritanceMachine(HsmState.A1);
        
//        // Move to A2 which overrides the ToB transition
//        machine.Fire(HsmTrigger.Next);
//        Assert.Equal(HsmState.A2, machine.CurrentState);
//        machine.ExecutionLog.Clear();

//        // Act - Fire trigger that's defined both on parent and child
//        machine.Fire(HsmTrigger.ToB);

//        // Assert - Child's transition should take precedence
//        Assert.Equal(HsmState.C, machine.CurrentState);
//        Assert.Contains("Action-A2-to-C", machine.ExecutionLog);
//        Assert.DoesNotContain("Action-A-to-B", machine.ExecutionLog);
//    }

//    [Fact]
//    public void InternalTransition_DoesNotChangeState()
//    {
//        // Arrange
//        var machine = new InternalTransitionMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act - Fire internal transition defined on parent
//        machine.Fire(HsmTrigger.Internal);

//        // Assert - State should not change, no exit/entry
//        Assert.Equal(HsmState.A1, machine.CurrentState);
//        Assert.Contains("Action-A-Internal", machine.ExecutionLog);
//        Assert.DoesNotContain("Exit-A1", machine.ExecutionLog);
//        Assert.DoesNotContain("Entry-A1", machine.ExecutionLog);
//        Assert.DoesNotContain("Exit-A", machine.ExecutionLog);
//        Assert.DoesNotContain("Entry-A", machine.ExecutionLog);
//    }

//    [Fact]
//    public void ExternalTransition_TriggersExitEntry()
//    {
//        // Arrange
//        var machine = new InternalTransitionMachine(HsmState.A1);
//        machine.ExecutionLog.Clear();

//        // Act - Fire external self-transition
//        machine.Fire(HsmTrigger.External);

//        // Assert - Should trigger exit and entry
//        Assert.Equal(HsmState.A1, machine.CurrentState);
//        Assert.Contains("Exit-A1", machine.ExecutionLog);
//        Assert.Contains("Entry-A1", machine.ExecutionLog);
//        Assert.Contains("Action-A1-External", machine.ExecutionLog);
        
//        // Parent should not be affected for self-transition
//        Assert.DoesNotContain("Exit-A", machine.ExecutionLog);
//        Assert.DoesNotContain("Entry-A", machine.ExecutionLog);
//    }

//    [Fact]
//    public void ExitEntrySequence_CorrectOrder()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.A2);
//        machine.ExecutionLog.Clear();

//        // Act - Transition from A2 to B2 (different branches)
//        machine.Fire(HsmTrigger.ToB2);

//        // Assert - Verify exit/entry sequence
//        var expected = new[]
//        {
//            "Exit-A2",   // Exit from leaf
//            "Exit-A",    // Exit from parent
//            "Entry-B",   // Enter new parent
//            "Entry-B2",  // Enter target leaf
//            "Action-D-to-B2"
//        };

//        // The actual implementation might differ, but the order should be:
//        // 1. Exit from current leaf up to LCA
//        // 2. Enter from LCA down to target
//        Assert.Contains("Exit-A2", machine.ExecutionLog);
//        Assert.Contains("Exit-A", machine.ExecutionLog);
//        Assert.Contains("Entry-B", machine.ExecutionLog);
//        Assert.Contains("Entry-B2", machine.ExecutionLog);

//        var exitA2Index = machine.ExecutionLog.IndexOf("Exit-A2");
//        var exitAIndex = machine.ExecutionLog.IndexOf("Exit-A");
//        var entryBIndex = machine.ExecutionLog.IndexOf("Entry-B");
//        var entryB2Index = machine.ExecutionLog.IndexOf("Entry-B2");

//        Assert.True(exitA2Index < exitAIndex, "Child exit before parent exit");
//        Assert.True(exitAIndex < entryBIndex, "All exits before entries");
//        Assert.True(entryBIndex < entryB2Index, "Parent entry before child entry");
//    }

//    [Fact]
//    public void IsIn_ChecksActiveStatePath()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.A2);

//        // Assert
//        Assert.True(machine.IsIn(HsmState.A2));  // Current state
//        Assert.True(machine.IsIn(HsmState.A));   // Parent
//        Assert.True(machine.IsIn(HsmState.Root)); // Root
//        Assert.False(machine.IsIn(HsmState.A1));  // Sibling
//        Assert.False(machine.IsIn(HsmState.B));   // Different branch
//    }

//    [Fact]
//    public void GetActivePath_ReturnsFullHierarchy()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.A2);

//        // Act
//        var path = machine.GetActivePath();

//        // Assert
//        Assert.Equal(3, path.Count);
//        Assert.Equal(HsmState.Root, path[0]);
//        Assert.Equal(HsmState.A, path[1]);
//        Assert.Equal(HsmState.A2, path[2]);
//    }

//    [Fact]
//    public void TransitionToSpecificSubstate_BypassesInitial()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.D);
//        machine.ExecutionLog.Clear();

//        // Act - Transition directly to A2 (not the initial child A1)
//        machine.Fire(HsmTrigger.ToA2);

//        // Assert
//        Assert.Equal(HsmState.A2, machine.CurrentState);
//        Assert.Contains("Entry-A", machine.ExecutionLog);
//        Assert.Contains("Entry-A2", machine.ExecutionLog);
//        Assert.DoesNotContain("Entry-A1", machine.ExecutionLog);
//    }

//    [Fact]
//    public void RootTransition_WorksFromAnyState()
//    {
//        // Test from different starting states
//        var states = new[] { HsmState.A1, HsmState.B2, HsmState.C1 };

//        foreach (var startState in states)
//        {
//            // Arrange
//            var machine = new BasicHierarchicalMachine(startState);
//            machine.ExecutionLog.Clear();

//            // Act - Fire transition defined on root
//            machine.Fire(HsmTrigger.ToD);

//            // Assert
//            Assert.Equal(HsmState.D, machine.CurrentState);
//            Assert.Contains("Action-Root-to-D", machine.ExecutionLog);
//        }
//    }

//    [Fact]
//    public void HasTransition_IncludesInheritedTransitions()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.A1);

//        // Assert - Should have transitions from self, parent A, and root
//        Assert.True(machine.HasTransition(HsmTrigger.Next));  // Defined on A1
//        Assert.True(machine.HasTransition(HsmTrigger.ToB));   // Defined on parent A
//        Assert.True(machine.HasTransition(HsmTrigger.ToD));   // Defined on root
//        Assert.False(machine.HasTransition(HsmTrigger.ToC1)); // Not accessible from A1
//    }

//    [Fact]
//    public void GetDefinedTriggers_IncludesInheritedTriggers()
//    {
//        // Arrange
//        var machine = new BasicHierarchicalMachine(HsmState.A1);

//        // Act
//        var triggers = machine.GetDefinedTriggers();

//        // Assert - Should include triggers from A1, A, and Root
//        Assert.Contains(HsmTrigger.Next, triggers);  // From A1
//        Assert.Contains(HsmTrigger.ToB, triggers);   // From A
//        Assert.Contains(HsmTrigger.ToD, triggers);   // From Root
//    }
//}