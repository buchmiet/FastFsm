using Abstractions.Attributes;
using Microsoft.Extensions.Logging;

namespace TestHsm
{
    public enum HState { A, A1, A2, B, B1 }
    public enum HTrigger { Refresh, MoveToA2, Switch, Back }

    [StateMachine(typeof(HState), typeof(HTrigger))]
    public partial class HsmMachine
    {
        public int Counter { get; private set; }

        // Define composite states and hierarchy
        [State(HState.A, Parent = null, Initial = HState.A1, HistoryMode = HistoryMode.Shallow)]
        [State(HState.A1, Parent = HState.A)]
        [State(HState.A2, Parent = HState.A)]
        [State(HState.B, Parent = null, Initial = HState.B1)]
        [State(HState.B1, Parent = HState.B)]
        private void ConfigureStates() { }

        // Transitions
        [Transition(HState.A1, HTrigger.MoveToA2, HState.A2)]
        [Transition(HState.A, HTrigger.Switch, HState.B)]
        [Transition(HState.B, HTrigger.Back, HState.A)]
        private void ConfigureTransitions() { }

        // Internal transition on composite state
        [InternalTransition(HState.A, HTrigger.Refresh, nameof(OnAncestorRefresh))]
        private void ConfigureInternal() { }

        private void OnAncestorRefresh() => Counter++;
    }
}