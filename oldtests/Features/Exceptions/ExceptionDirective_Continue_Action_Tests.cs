using System;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using FastFsm.Exceptions;
using Xunit;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Continue_Action_Tests
{
    [Fact]
    public void ActionThrow_Continue_Swallows_StateChanged()
    {
        var m = new Machines.ContinueOnActionMachine(ASState.A);
        m.Start();

        Assert.Equal(ASState.A, m.CurrentState);

        m.Fire(ASTrigger.Go);

        Assert.Equal(ASState.B, m.CurrentState);
    }
}
