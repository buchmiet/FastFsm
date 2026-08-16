namespace Machines.Tests.Machines.Legacy;

// HSM machine - Fluent version
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
public partial class HsmMachine
{
    public int Counter { get; private set; }

    // Define composite states and hierarchy
    [State(HState.A, History = HistoryMode.Shallow)]
    [State(HState.A1, Parent = HState.A, IsInitial = true)]
    [State(HState.A2, Parent = HState.A)]
    [State(HState.B)]
    [State(HState.B1, Parent = HState.B, IsInitial = true)]
    private void DefineStates() { }

    // Internal transition on ancestor A
    [InternalTransition(HState.A, HTrigger.Refresh, nameof(OnAncestorRefresh))]
    private void DefineAncestorInternal() { }

    // External transitions
    [Transition(HState.A1, HTrigger.MoveToA2, HState.A2, Guard = nameof(Always))]
    [Transition(HState.A, HTrigger.Switch, HState.B, Guard = nameof(Always))]
    [Transition(HState.B, HTrigger.Back, HState.A, Guard = nameof(Always))]
    private void DefineTransitions() { }

    private void OnAncestorRefresh() => Counter++;
    private bool Always() => true;
}