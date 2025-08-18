using Abstractions.Attributes;

namespace CleanUp;

// Basic state machine without guards or actions
public enum BasicState
{
    Idle,
    Processing,
    Completed,
    Failed
}

public enum BasicTrigger
{
    Start,
    Complete,
    Fail,
    Reset
}

[StateMachine(typeof(BasicState), typeof(BasicTrigger))]
public partial class BasicStateMachine
{
    [Transition(BasicState.Idle, BasicTrigger.Start, BasicState.Processing)]
    [Transition(BasicState.Processing, BasicTrigger.Complete, BasicState.Completed)]
    [Transition(BasicState.Processing, BasicTrigger.Fail, BasicState.Failed)]
    [Transition(BasicState.Completed, BasicTrigger.Reset, BasicState.Idle)]
    [Transition(BasicState.Failed, BasicTrigger.Reset, BasicState.Idle)]
    private void ConfigureTransitions() { }
}