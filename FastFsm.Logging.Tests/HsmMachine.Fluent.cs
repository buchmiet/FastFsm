using Abstractions.Fluent;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

public enum HState { A, A1, A2, B, B1 }
public enum HTrigger { Refresh, MoveToA2, Switch, Back }

// HSM machine - Fluent version
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
public partial class HsmMachineFluent
{
    public int Counter { get; private set; }

    [State(HState.A, History = Abstractions.Attributes.HistoryMode.Shallow)]
    [State(HState.A1, Parent = HState.A, IsInitial = true)]
    [State(HState.A2, Parent = HState.A)]
    [State(HState.B)]
    [State(HState.B1, Parent = HState.B, IsInitial = true)]
    private void DefineStates() { }

    [InternalTransition(HState.A, HTrigger.Refresh, nameof(OnAncestorRefresh))]
    private void DefineAncestorInternal() { }

    [Transition(HState.A1, HTrigger.MoveToA2, HState.A2, Guard = nameof(Always))]
    [Transition(HState.A, HTrigger.Switch, HState.B, Guard = nameof(Always))]
    [Transition(HState.B, HTrigger.Back, HState.A, Guard = nameof(Always))]
    private void DefineTransitions() { }

    private void OnAncestorRefresh() => Counter++;
    private bool Always() => true;
}

// HSM machine - Legacy attribute version (parity matrix)
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
public partial class HsmMachine
{
    public int Counter { get; private set; }

    [State(HState.A, History = Abstractions.Attributes.HistoryMode.Shallow)]
    [State(HState.A1, Parent = HState.A, IsInitial = true)]
    [State(HState.A2, Parent = HState.A)]
    [State(HState.B)]
    [State(HState.B1, Parent = HState.B, IsInitial = true)]
    private void DefineStates() { }

    [InternalTransition(HState.A, HTrigger.Refresh, nameof(OnAncestorRefresh))]
    private void DefineAncestorInternal() { }

    [Transition(HState.A1, HTrigger.MoveToA2, HState.A2, Guard = nameof(Always))]
    [Transition(HState.A, HTrigger.Switch, HState.B, Guard = nameof(Always))]
    [Transition(HState.B, HTrigger.Back, HState.A, Guard = nameof(Always))]
    private void DefineTransitions() { }

    private void OnAncestorRefresh() => Counter++;
    private bool Always() => true;
}
