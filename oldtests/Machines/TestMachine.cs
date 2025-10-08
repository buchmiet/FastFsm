using Abstractions.Attributes;
using FastFsm.Tests.Features.Exceptions;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class TestMachine
{
    [Transition(State.Initial, Trigger.Next, State.Final)]
    public void MoveToNextState() { }
}