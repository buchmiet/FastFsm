using Abstractions.Attributes;

namespace Examples
{
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class SimpleMachine
    {
        [Transition(State.A, Trigger.X, State.B)]
        private void Configure() { }
    }
}
