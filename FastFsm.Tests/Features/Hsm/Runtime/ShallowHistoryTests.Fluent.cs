using Xunit;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.Runtime;

public partial class ShallowHistoryTestsFluent
{
    [Fact]
    public void Reentering_Parent_With_ShallowHistory_Restores_LastChildFluent()
    {
        var m = new ShallowHistoryMachineFluent(S.Outside);
        m.Start();

        // Enter parent → initial child
        m.Fire(T.Enter);
        Assert.Equal(S.Menu_Main, m.CurrentState);

        // Move to another child
        m.Fire(T.Next);
        Assert.Equal(S.Menu_Settings, m.CurrentState);

        // Exit composite
        m.Fire(T.Exit);
        Assert.Equal(S.Outside, m.CurrentState);

        // Re-enter → shallow history brings us back to Settings
        m.Fire(T.Enter);
        Assert.Equal(S.Menu_Settings, m.CurrentState);
    }

    public enum S { Outside, Menu, Menu_Main, Menu_Settings }
    public enum T { Enter, Next, Back, Exit }

    [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
    public partial class ShallowHistoryMachineFluent
    {
        private void Configure()
        {
            // Define parent state with shallow history and initial child
            FSM.State(S.Menu)
                .Initial(S.Menu_Main)
                .HistoryShallow()
                .On(T.Exit).GoTo(S.Outside);

            // Define child states
            FSM.State(S.Menu_Main)
                .ChildOf(S.Menu)
                .On(T.Next).GoTo(S.Menu_Settings);

            FSM.State(S.Menu_Settings)
                .ChildOf(S.Menu)
                .On(T.Back).GoTo(S.Menu_Main);

            // Define outside state
            FSM.State(S.Outside)
                .On(T.Enter).GoTo(S.Menu);
        }
    }
}