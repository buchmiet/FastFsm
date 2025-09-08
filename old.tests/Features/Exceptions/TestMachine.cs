using Abstractions.Attributes;

namespace FastFsm.Tests.Features.Exceptions
{
    /// <summary>
    /// Definicja prostej maszyny stanu używanej w testach.
    /// Generator FSM.NET stworzy na jej podstawie klasę partial z całą logiką.
    /// </summary>
    [StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]

    public partial class TestMachine
    {
        [Transition(State.Initial, Trigger.Next, State.Final)]
        public void MoveToNextState() { }
    }
    public enum State { Initial, Final }
    public enum Trigger { Next }
}
