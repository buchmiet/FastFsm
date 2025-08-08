using System;
using Abstractions.Attributes;
using StateMachine.Exceptions;
using Xunit;

namespace StateMachine.Tests.BasicVariant;

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

public enum PSState { A, B }
public enum PSTrigger { Go }

[StateMachine(typeof(PSState), typeof(PSTrigger))]
[OnException(nameof(Handle))]
public partial class PropagateOnActionMachine
{
    [Transition(PSState.A, PSTrigger.Go, PSState.B, Action = nameof(DoWork))]
    private void Configure() { }

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<PSState, PSTrigger> ctx)
        => ExceptionDirective.Propagate;
}