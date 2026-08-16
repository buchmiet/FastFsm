using Abstractions.Attributes;

namespace Demo;

public enum State
{
    Idle,
    Running
}

public enum Trigger
{
    Start
}

[StateMachine(typeof(State), typeof(Trigger))]
public partial class SimpleMachine
{
    [Transition(State.Idle, Trigger.Start, State.Running)]
    private void Configure()
    {
        // No additional configuration required
    }
}
