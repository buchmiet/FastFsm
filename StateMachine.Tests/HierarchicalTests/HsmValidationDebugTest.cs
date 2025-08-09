using Abstractions.Attributes;

namespace StateMachine.Tests.HierarchicalTests;

// Test 1: Oczywisty cykl A->A (samo-referencja)
public enum SelfCycleStates { A }
public enum TestTriggers { Go }

[StateMachine(typeof(SelfCycleStates), typeof(TestTriggers), EnableHierarchy = true)]
public partial class SelfCycleHsm
{
    // A wskazuje na siebie - powinno wywołać FSM100
    [State(SelfCycleStates.A, Parent = SelfCycleStates.A)]
    private void ConfigureStates() { }
}

// Test 2: Prosty cykl A->B->A
public enum SimpleCycleStates { A, B }

[StateMachine(typeof(SimpleCycleStates), typeof(TestTriggers), EnableHierarchy = true)]
public partial class SimpleCycleHsm
{
    [State(SimpleCycleStates.A, Parent = SimpleCycleStates.B)]
    private void ConfigureA() { }
    
    [State(SimpleCycleStates.B, Parent = SimpleCycleStates.A)]
    private void ConfigureB() { }
}

// Test 3: Orphan - parent jako nieprawidłowa wartość enum
public enum OrphanStates { Child }

[StateMachine(typeof(OrphanStates), typeof(TestTriggers), EnableHierarchy = true)]
public partial class OrphanHsm
{
    // 999 nie istnieje w enum - powinno wywołać FSM101
    [State(OrphanStates.Child, Parent = (OrphanStates)999)]
    private void ConfigureStates() { }
}

// Test 4: Multiple initial substates - najprostszy przypadek
public enum MultiInitStates { Parent, Child1, Child2 }

[StateMachine(typeof(MultiInitStates), typeof(TestTriggers), EnableHierarchy = true)]
public partial class MultipleInitialsSimpleHsm
{
    [State(MultiInitStates.Parent)]
    private void ConfigureParent() { }
    
    // Oba dzieci są initial - powinno wywołać FSM103
    [State(MultiInitStates.Child1, Parent = MultiInitStates.Parent, IsInitial = true)]
    private void ConfigureChild1() { }
    
    [State(MultiInitStates.Child2, Parent = MultiInitStates.Parent, IsInitial = true)]
    private void ConfigureChild2() { }
}

// Test 5: History na prostym stanie - najprostszy przypadek
public enum SimpleHistoryStates { SimpleState }

[StateMachine(typeof(SimpleHistoryStates), typeof(TestTriggers), EnableHierarchy = true)]
public partial class SimpleHistoryHsm
{
    // Stan bez dzieci z historią - powinno wywołać FSM104
    [State(SimpleHistoryStates.SimpleState, History = HistoryMode.Shallow)]
    private void ConfigureSimple() { }
}