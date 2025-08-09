using Abstractions.Attributes;

namespace StateMachine.Tests.HierarchicalTests;

// Najprostszy możliwy cykl: A->A
public enum SelfCycleStates { StateA }
public enum SelfCycleTriggers { Go }

[StateMachine(typeof(SelfCycleStates), typeof(SelfCycleTriggers), EnableHierarchy = true)]
public partial class SelfCycleHsm
{
    // Stan wskazuje na siebie jako rodzica - oczywisty cykl
    [State(SelfCycleStates.StateA, Parent = SelfCycleStates.StateA)]
    private void ConfigureA() { }
}