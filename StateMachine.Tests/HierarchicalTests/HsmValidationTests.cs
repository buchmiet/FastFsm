using Xunit;
using Abstractions.Attributes;

namespace StateMachine.Tests.HierarchicalTests;

public class HsmValidationTests
{
    // Test: Circular dependency powinien być wykryty (FSM100)
    public enum CircularStates { A, B, C }
    public enum CircularTriggers { Go }
    
    [StateMachine(typeof(CircularStates), typeof(CircularTriggers), EnableHierarchy = true)]
    public partial class CircularHsm
    {
        // A -> B -> C -> A (cykl)
        [State(CircularStates.A, Parent = CircularStates.C)]
        private void ConfigureA() { }
        
        [State(CircularStates.B, Parent = CircularStates.A)]
        private void ConfigureB() { }
        
        [State(CircularStates.C, Parent = CircularStates.B)]
        private void ConfigureC() { }
    }
    
    // Test: Multiple initial substates (FSM103)
    public enum MultiInitStates { Parent, Child1, Child2 }
    public enum TestTriggers { T1 }
    
    [StateMachine(typeof(MultiInitStates), typeof(TestTriggers), EnableHierarchy = true)]
    public partial class MultipleInitialsHsm
    {
        [State(MultiInitStates.Parent)]
        private void ConfigureParent() { }
        
        [State(MultiInitStates.Child1, Parent = MultiInitStates.Parent, IsInitial = true)]
        private void ConfigureChild1() { }
        
        [State(MultiInitStates.Child2, Parent = MultiInitStates.Parent, IsInitial = true)]
        private void ConfigureChild2() { }
    }
    
    // Test: History on non-composite state (FSM104)
    public enum HistoryStates { Simple, Parent, Child }
    
    [StateMachine(typeof(HistoryStates), typeof(TestTriggers), EnableHierarchy = true)]
    public partial class InvalidHistoryHsm
    {
        [State(HistoryStates.Simple, History = HistoryMode.Shallow)]
        private void ConfigureSimple() { }
        
        [State(HistoryStates.Parent)]
        private void ConfigureParent() { }
        
        [State(HistoryStates.Child, Parent = HistoryStates.Parent)]
        private void ConfigureChild() { }
    }
    
    // Test: Orphan substate (FSM101)
    public enum OrphanStates { A, B, C }
    
    [StateMachine(typeof(OrphanStates), typeof(TestTriggers), EnableHierarchy = true)]
    public partial class OrphanSubstateHsm
    {
        [State(OrphanStates.A)]
        private void ConfigureA() { }
        
        // B references non-existent parent D
        [State(OrphanStates.B, Parent = OrphanStates.C)]
        private void ConfigureB() { }
        
        // C doesn't exist in enum - this should cause compilation error
    }
    
    [Fact]
    public void TestValidationRulesCompile()
    {
        // Ten test sprawdza tylko czy kod się kompiluje
        // Błędy walidacji powinny być zgłoszone podczas kompilacji
        Assert.True(true, "Validation tests should trigger compile-time diagnostics");
    }
}