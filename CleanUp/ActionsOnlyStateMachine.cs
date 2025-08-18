using Abstractions.Attributes;

namespace CleanUp;

public enum A1State { Idle, Work, Done }
public enum A1Trigger { Start, Finish, Reset }

[StateMachine(typeof(A1State), typeof(A1Trigger))]
public partial class ActionsOnlyStateMachine
{
    // States with callbacks
    [State(A1State.Work, OnEntry = nameof(OnEnterWork), OnExit = nameof(OnExitWork))]
    [State(A1State.Done, OnEntry = nameof(OnEnterDone))]
    private void ConfigureStates() { }

    // Transitions with actions
    [Transition(A1State.Idle, A1Trigger.Start, A1State.Work, Action = nameof(Begin))]
    [Transition(A1State.Work, A1Trigger.Finish, A1State.Done, Action = nameof(WrapUp))]
    [Transition(A1State.Work, A1Trigger.Reset, A1State.Idle)]
    [Transition(A1State.Done, A1Trigger.Reset, A1State.Idle)]
    private void ConfigureTransitions() { }

    private void OnEnterWork() { }
    private void OnExitWork()  { }
    private void OnEnterDone() { }

    private void Begin() { }     // Idle->Work (Start)
    private void WrapUp() { }    // Work->Done (Finish)
}

