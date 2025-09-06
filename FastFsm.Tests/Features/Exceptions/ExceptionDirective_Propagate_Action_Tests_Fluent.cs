using System;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Propagate_Action_Tests_Fluent
{
    [Fact]
    public void ActionThrow_Propagate_Throws_StateChanged_Fluent()
    {
        var m = new PropagateOnActionMachine_Fluent(PSState.A);
        m.Start();

        Assert.Throws<InvalidOperationException>(() => m.Fire(PSTrigger.Go));

        Assert.Equal(PSState.B, m.CurrentState);
    }
}

[StateMachine(typeof(PSState), typeof(PSTrigger))]
public partial class PropagateOnActionMachine_Fluent
{
    private static void Configure() => FSM
        .OnException<PSState>(nameof(Handle))
        .State(PSState.A)
            .On(PSTrigger.Go).Action(nameof(DoWork)).GoTo(PSState.B)
        .State(PSState.B);

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<PSState, PSTrigger> ctx)
        => ExceptionDirective.Propagate;
}
