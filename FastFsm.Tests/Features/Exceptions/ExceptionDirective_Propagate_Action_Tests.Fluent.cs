using System;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Propagate_Action_Tests
{
    [Fact]
    public void ActionThrow_Propagate_Throws_StateChanged()
    {
        var m = new PropagateOnActionMachineFluent(PSState.A);
        m.Start();

        Assert.Throws<InvalidOperationException>(() => m.Fire(PSTrigger.Go));

        Assert.Equal(PSState.B, m.CurrentState);
    }
}

[StateMachine(typeof(PSState), typeof(PSTrigger))]
public partial class PropagateOnActionMachineFluent
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
