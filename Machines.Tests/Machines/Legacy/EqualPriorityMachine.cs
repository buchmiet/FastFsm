using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(EP_State), typeof(EP_Trigger), EnableHierarchy = true)]
public partial class EqualPriorityMachine
{
    // Two transitions with the same priority from the same state/trigger.
    // Should compile; generator may warn but must not fail.
    [Transition(EP_State.A, EP_Trigger.Go, EP_State.B, Priority = 100, Action = (ActionB))]
    [Transition(EP_State.A, EP_Trigger.Go, EP_State.C, Priority = 100, Action = (ActionC))]
    private void T_Priority() { }

    private void ActionB() { }
    private void ActionC() { }
}
