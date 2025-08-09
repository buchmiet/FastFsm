using Abstractions.Attributes;

namespace StateMachine.Tests.HierarchicalTests;

// ====== TEST FSM101: Orphan Substate ======
public enum OrphanStates { Child, Orphan }  // Parent NIE istnieje w enum!
public enum TestTrigger { Go }

[StateMachine(typeof(OrphanStates), typeof(TestTrigger), EnableHierarchy = true)]
public partial class OrphanTest
{
    // Child wskazuje na Parent który nie istnieje jako stan
    [State(OrphanStates.Child)]
    private void ConfigureChild() { }
    
    // Orphan też nie ma rodzica
    [State(OrphanStates.Orphan)]
    private void ConfigureOrphan() { }
    
    // Ten test nie może użyć OrphanStates.Parent bo nie istnieje w enum
    // Więc FSM101 powinno być wykryte tylko jeśli ktoś ręcznie edytuje wygenerowany kod
}

// ====== TEST FSM102: Invalid Hierarchy Configuration ======
public enum NoInitialStates { Parent, Child1, Child2 }

[StateMachine(typeof(NoInitialStates), typeof(TestTrigger), EnableHierarchy = true)]
public partial class NoInitialSubstateTest
{
    [State(NoInitialStates.Parent)]
    private void ConfigureParent() { }
    
    // Dzieci bez żadnego IsInitial = true
    [State(NoInitialStates.Child1, Parent = NoInitialStates.Parent)]
    private void ConfigureChild1() { }
    
    [State(NoInitialStates.Child2, Parent = NoInitialStates.Parent)]
    private void ConfigureChild2() { }
}

// ====== TEST FSM104: Invalid History Configuration ======
public enum HistoryStates { Simple, Composite, Child }

[StateMachine(typeof(HistoryStates), typeof(TestTrigger), EnableHierarchy = true)]
public partial class InvalidHistoryTest
{
    // Simple nie ma dzieci ale ma History
    [State(HistoryStates.Simple, History = HistoryMode.Shallow)]
    private void ConfigureSimple() { }
    
    // Composite ma dzieci i History - to jest OK
    [State(HistoryStates.Composite, History = HistoryMode.Deep)]
    private void ConfigureComposite() { }
    
    [State(HistoryStates.Child, Parent = HistoryStates.Composite, IsInitial = true)]
    private void ConfigureChild() { }
}

// ====== TEST: Poprawna hierarchia (nie powinno być błędów) ======
public enum ValidStates { Root, Parent, Child1, Child2 }

[StateMachine(typeof(ValidStates), typeof(TestTrigger), EnableHierarchy = true)]
public partial class ValidHierarchyTest
{
    [State(ValidStates.Root)]
    private void ConfigureRoot() { }
    
    [State(ValidStates.Parent, History = HistoryMode.Shallow)]
    private void ConfigureParent() { }
    
    [State(ValidStates.Child1, Parent = ValidStates.Parent, IsInitial = true)]
    private void ConfigureChild1() { }
    
    [State(ValidStates.Child2, Parent = ValidStates.Parent)]
    private void ConfigureChild2() { }
    
    [Transition(ValidStates.Root, TestTrigger.Go, ValidStates.Parent)]
    private void ConfigureTransition() { }
}