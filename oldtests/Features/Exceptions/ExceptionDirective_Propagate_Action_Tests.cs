using System;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using Xunit;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Propagate_Action_Tests
{
    [Fact]
    public void ActionThrow_Propagate_Throws_StateChanged()
    {
        var m = new Machines.PropagateOnActionMachine(PSState.A);
        m.Start();

        Assert.Throws<InvalidOperationException>(() => m.Fire(PSTrigger.Go));

        Assert.Equal(PSState.B, m.CurrentState);
    }
}
