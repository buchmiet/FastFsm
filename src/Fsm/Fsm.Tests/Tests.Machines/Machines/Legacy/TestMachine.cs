using Abstractions.Attributes;
namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(BasicState), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class TestMachine
{
    [Transition(BasicState.Initial, Trigger.Next, BasicState.Final)]
    public void MoveToNextState() { }
}
