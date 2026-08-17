using System;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Exceptions;

public class ExceptionDirective_Propagate_Action_Tests
{
    [Fact]
    public void ActionThrow_Propagate_Throws_StateChanged()
    {
        var m = new PropagateOnActionMachine(PSState.A);
        m.Start();

        Assert.Throws<InvalidOperationException>(() => m.Fire(PSTrigger.Go));

        Assert.Equal(PSState.B, m.CurrentState);
    }
}
