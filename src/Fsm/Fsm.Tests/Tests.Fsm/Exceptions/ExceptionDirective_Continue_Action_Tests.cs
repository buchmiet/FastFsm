using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Exceptions;

public class ExceptionDirective_Continue_Action_Tests
{
    [Fact]
    public void ActionThrow_Continue_Swallows_StateChanged()
    {
        var m = new ContinueOnActionMachine(ASState.A);
        m.Start();

        Assert.Equal(ASState.A, m.CurrentState);

        m.Fire(ASTrigger.Go);

        Assert.Equal(ASState.B, m.CurrentState);
    }
}
