using Xunit;

namespace FastFsm.Tests.Features.Hsm.Runtime;

public partial class InitialChildTestsLegacy
{
    [Fact]
    public void Transition_ToCompositeParent_Enters_ItsInitialChildLegacy()
    {
        var m = new InitialChildMachineLegacy(InitialChildTestsFluent.S.Outside);
        m.Start();

        Assert.Equal(InitialChildTestsFluent.S.Outside, m.CurrentState);

        m.Fire(InitialChildTestsFluent.T.EnterParent);
        Assert.Equal(InitialChildTestsFluent.S.Parent_A, m.CurrentState); // auto-descend to initial child

        m.Fire(InitialChildTestsFluent.T.Switch);
        Assert.Equal(InitialChildTestsFluent.S.Parent_B, m.CurrentState);

        m.Fire(InitialChildTestsFluent.T.LeaveParent);
        Assert.Equal(InitialChildTestsFluent.S.Outside, m.CurrentState);
    }

    [StateMachine(typeof(InitialChildTestsFluent.S), typeof(InitialChildTestsFluent.T), EnableHierarchy = true)]
    public partial class InitialChildMachineLegacy
    {
        // Define parent state
        [State(InitialChildTestsFluent.S.Parent)]
        private void ConfigureParent() { }

        // Define child states
        [State(InitialChildTestsFluent.S.Parent_A, Parent = InitialChildTestsFluent.S.Parent, IsInitial = true)]
        private void ConfigureParentA() { }

        [State(InitialChildTestsFluent.S.Parent_B, Parent = InitialChildTestsFluent.S.Parent)]
        private void ConfigureParentB() { }

        // Define outside state
        [State(InitialChildTestsFluent.S.Outside)]
        private void ConfigureOutside() { }

        // Transitions
        [Transition(InitialChildTestsFluent.S.Outside, InitialChildTestsFluent.T.EnterParent, InitialChildTestsFluent.S.Parent)]
        private void ConfigureOutsideToParent() { }

        [Transition(InitialChildTestsFluent.S.Parent_A, InitialChildTestsFluent.T.Switch, InitialChildTestsFluent.S.Parent_B)]
        private void ConfigureParentAToParentB() { }

        [Transition(InitialChildTestsFluent.S.Parent, InitialChildTestsFluent.T.LeaveParent, InitialChildTestsFluent.S.Outside)]
        private void ConfigureParentToOutside() { }
    }
}