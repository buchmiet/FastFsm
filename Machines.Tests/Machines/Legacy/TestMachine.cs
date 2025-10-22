namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class TestMachine
{
    [Transition(State.Initial, Trigger.Next, State.Final)]
    public void MoveToNextState() { }
}