using Abstractions.Attributes;

namespace CleanUp;

public enum IState { A, B }
public enum ITrigger { Next, Reset }

[StateMachine(typeof(IState), typeof(ITrigger))]
public partial class InternalOnlyStateMachine
{
    [State(IState.B, OnEntry = nameof(OnEnterB))]
    private void ConfigureStates() { }

    [Transition(IState.A, ITrigger.Next, IState.B)]
    [Transition(IState.B, ITrigger.Reset, IState.A)]
    private void ConfigureTransitions() { }

    private void OnEnterB() { }
}
