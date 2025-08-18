using Abstractions.Attributes;

namespace CleanUp;

public enum G1State { Idle, Ready, Error }
public enum G1Trigger { Check, Reset }

[StateMachine(typeof(G1State), typeof(G1Trigger))]
public partial class GuardsOnlyStateMachine
{
    // Transitions
    [Transition(G1State.Idle, G1Trigger.Check, G1State.Ready, Guard = nameof(CanGo))]
    [Transition(G1State.Ready, G1Trigger.Reset, G1State.Idle)]
    [Transition(G1State.Error, G1Trigger.Reset, G1State.Idle)]
    private void ConfigureTransitions() { }

    // Guard
    private bool CanGo() => true;
}

