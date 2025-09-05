using System;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Continue_Action_Tests_Fluent
{
    [Fact]
    public void ActionThrow_Continue_Swallows_StateChanged_Fluent()
    {
        var m = new ContinueOnActionMachine_Fluent(ASState.A);
        m.Start();

        Assert.Equal(ASState.A, m.CurrentState);

        m.Fire(ASTrigger.Go);

        Assert.Equal(ASState.B, m.CurrentState);
    }
}

[StateMachine(typeof(ASState), typeof(ASTrigger))]
public partial class ContinueOnActionMachine_Fluent
{
    private static void Configure() => FSM
        .State<ASState>(ASState.A)
            .OnException(nameof(Handle))
            .On(ASTrigger.Go).Action(nameof(DoWork)).GoTo(ASState.B)
        .State(ASState.B);

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<ASState, ASTrigger> ctx)
        => ExceptionDirective.Continue;
}