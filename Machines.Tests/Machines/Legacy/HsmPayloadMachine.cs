using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

public class PayloadData { public int Value { get; set; } }

[StateMachine(typeof(HP_State), typeof(HP_Trigger), EnableHierarchy = true)]
[PayloadType(typeof(PayloadData))]
public partial class HsmPayloadMachine
{
    [State(HP_State.Root)] private void S_Root() { }
    [State(HP_State.ChildA, Parent = HP_State.Root, IsInitial = true)] private void S_ChildA() { }
    [State(HP_State.ChildB, Parent = HP_State.Root)] private void S_ChildB() { }

    // Internal (no state change) with payload
    [InternalTransition(HP_State.ChildA, HP_Trigger.Configure, Action = (ConfigureAction))]
    // External with payload (guard + action)
    [Transition(HP_State.ChildA, HP_Trigger.Submit, HP_State.ChildB, Guard = (CanSubmit), Action = (SubmitAction))]
    private void T_All() { }

    private void ConfigureAction(PayloadData p) { }
    private void SubmitAction(PayloadData p) { }
    private bool CanSubmit(PayloadData p) => true;
}
