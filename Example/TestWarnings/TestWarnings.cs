using Abstractions.Attributes;

namespace Example.TestWarnings;

// This should NOT generate FSM004 - no FSM attributes at all
public partial class RegularPartialClass
{
    public void DoSomething() { }
}

// This should NOT generate FSM004 - no FSM attributes at all  
public partial class TestHelper
{
    public int Calculate(int a, int b) => a + b;
}

// This SHOULD generate FSM004 - has Transition attribute but no StateMachine
public partial class IncompleteStateMachine
{
    [Transition(States.Idle, Triggers.Start, States.Running)]
    private void ConfigureTransitions() { }
}

// This SHOULD generate FSM004 - has State attribute but no StateMachine
public partial class AnotherIncomplete
{
    [State(States.Idle, OnEntry = nameof(OnIdle))]
    private void ConfigureStates() { }
    
    private void OnIdle() { }
}

// This should NOT generate FSM004 - properly configured
[StateMachine(typeof(States), typeof(Triggers))]
public partial class ProperStateMachine
{
    [Transition(States.Idle, Triggers.Start, States.Running)]
    private void ConfigureTransitions() { }
}

public enum States { Idle, Running }
public enum Triggers { Start, Stop }