using System;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using FastFsm.Tests.Machines;
using Xunit;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Propagate_Action_Tests
{
    [Fact]
    public void ActionThrow_Propagate_Throws_StateChanged()
    {
        var m = new Machines.Legacy.PropagateOnActionMachine(PSState.A);
        m.Start();

        Assert.Throws<InvalidOperationException>(() => m.Fire(PSTrigger.Go));

        Assert.Equal(PSState.B, m.CurrentState);
    }
}
