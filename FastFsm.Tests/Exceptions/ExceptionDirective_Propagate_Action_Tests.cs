using System;
using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;

namespace FastFsm.Tests.Exceptions;

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
