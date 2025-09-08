using System;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

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

public enum ASState { A, B }
public enum ASTrigger { Go }

[StateMachine(typeof(ASState), typeof(ASTrigger))]
[OnException(nameof(Handle))]
public partial class ContinueOnActionMachine
{
    [Transition(ASState.A, ASTrigger.Go, ASState.B, Action = nameof(DoWork))]
    private void Configure() { }

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<ASState, ASTrigger> ctx)
        => ExceptionDirective.Continue;
}
