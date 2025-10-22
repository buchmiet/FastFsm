namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(InheritanceMachine_S), typeof(InheritanceMachine_T), EnableHierarchy = true)]
public partial class InheritanceMachine
{
    [State(InheritanceMachine_S.Parent)] private void Parent() { }
    [State(InheritanceMachine_S.Parent_A, Parent = InheritanceMachine_S.Parent, IsInitial = true)] private void A() { }
    [State(InheritanceMachine_S.Parent_B, Parent = InheritanceMachine_S.Parent)] private void B() { }

    // Parent‑level transition that applies from any child
    [Transition(InheritanceMachine_S.Parent, InheritanceMachine_T.Leave, InheritanceMachine_S.Outside)]
    // Child‑only transition
    [Transition(InheritanceMachine_S.Parent_A, InheritanceMachine_T.Next, InheritanceMachine_S.Parent_B)]
    // Enter composite from outside
    [Transition(InheritanceMachine_S.Outside, InheritanceMachine_T.Enter, InheritanceMachine_S.Parent)]
    private void Configure() { }
}
