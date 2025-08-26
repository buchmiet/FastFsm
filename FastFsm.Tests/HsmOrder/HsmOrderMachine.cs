using Abstractions.Attributes;

namespace FastFsm.Tests.HsmOrder
{
    public enum HState { A, A1, A2, B, B1 }
    public enum HTrigger { MoveToA2, Switch, Back }

    [StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
    public partial class HsmOrderMachine
    {
        [State(HState.A, History = HistoryMode.Shallow)]
        [State(HState.A1, Parent = HState.A, IsInitial = true)]
        [State(HState.A2, Parent = HState.A)]
        [State(HState.B)]
        [State(HState.B1, Parent = HState.B, IsInitial = true)]
        private void DefineStates() { }

        private bool Always() => true;

        [Transition(HState.A1, HTrigger.MoveToA2, HState.A2, Guard = nameof(Always))]
        [Transition(HState.A, HTrigger.Switch, HState.B, Guard = nameof(Always))]
        [Transition(HState.B, HTrigger.Back, HState.A, Guard = nameof(Always))]
        private void DefineTransitions() { }
    }
}