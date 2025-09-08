using System;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Continue_Action_Tests
{
    [Fact]
    public void ActionThrow_Continue_Swallows_StateChanged()
    {
        var m = new ContinueOnActionMachineFluent(ASState.A);
        m.Start();

        Assert.Equal(ASState.A, m.CurrentState);

        m.Fire(ASTrigger.Go);

        Assert.Equal(ASState.B, m.CurrentState);
    }
}

[StateMachine(typeof(ASState), typeof(ASTrigger))]
public partial class ContinueOnActionMachineFluent
{
    private static void Configure() => FSM
        .OnException<ASState>(nameof(Handle))
        .State(ASState.A)
            .On(ASTrigger.Go).Action(nameof(DoWork)).GoTo(ASState.B)
        .State(ASState.B);

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<ASState, ASTrigger> ctx)
        => ExceptionDirective.Continue;
}
