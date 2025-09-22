using Xunit;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.Runtime;

public partial class InitialChildTestsFluent
{
    [Fact]
    public void Transition_ToCompositeParent_Enters_ItsInitialChildFluent()
    {
        var m = new InitialChildMachineFluent(S.Outside);
        m.Start();

        Assert.Equal(S.Outside, m.CurrentState);

        m.Fire(T.EnterParent);
        Assert.Equal(S.Parent_A, m.CurrentState); // auto-descend to initial child

        m.Fire(T.Switch);
        Assert.Equal(S.Parent_B, m.CurrentState);

        m.Fire(T.LeaveParent);
        Assert.Equal(S.Outside, m.CurrentState);
    }

    // Local enums for this test - same for both Fluent and Legacy
    public enum S { Outside, Parent, Parent_A, Parent_B }
    public enum T { EnterParent, Switch, LeaveParent }

    [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
    public partial class InitialChildMachineFluent
    {
        private void Configure()
        {
            // Define parent state with its initial child
            FSM.State(S.Parent)
                .Initial(S.Parent_A)
                .On(T.LeaveParent).GoTo(S.Outside);

            // Define child states
            FSM.State(S.Parent_A)
                .ChildOf(S.Parent)
                .On(T.Switch).GoTo(S.Parent_B);

            FSM.State(S.Parent_B)
                .ChildOf(S.Parent);

            // Define outside state
            FSM.State(S.Outside)
                .On(T.EnterParent).GoTo(S.Parent);
        }
    }
}