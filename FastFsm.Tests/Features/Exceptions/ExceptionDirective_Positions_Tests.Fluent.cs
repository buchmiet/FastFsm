using System;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Positions_Tests
{
    [Fact]
    public void OnException_InMiddle_Continue_Swallows()
    {
        var m = new MiddlePositionMachineFluent(MPState.A);
        m.Start();
        m.Fire(MPTrigger.Go);
        Assert.Equal(MPState.B, m.CurrentState);
    }

    [Fact]
    public void OnException_AtEnd_Continue_Swallows()
    {
        var m = new EndPositionMachineFluent(EPState.A);
        m.Start();
        m.Fire(EPTrigger.Go);
        Assert.Equal(EPState.B, m.CurrentState);
    }
}

[StateMachine(typeof(MPState), typeof(MPTrigger))]
public partial class MiddlePositionMachineFluent
{
    private static void Configure() => FSM
        .State(MPState.A)
            .On(MPTrigger.Go).Action(nameof(Throw)).GoTo(MPState.B)
        .OnException(nameof(Handle))
        .State(MPState.B);

    private void Throw() => throw new InvalidOperationException("boom");
    private ExceptionDirective Handle(ExceptionContext<MPState, MPTrigger> ctx) => ExceptionDirective.Continue;
}

public enum MPState { A, B }
public enum MPTrigger { Go }

[StateMachine(typeof(EPState), typeof(EPTrigger))]
public partial class EndPositionMachineFluent
{
    private static void Configure() => FSM
        .State(EPState.A)
            .On(EPTrigger.Go).Action(nameof(Throw)).GoTo(EPState.B)
        .State(EPState.B)
        .OnException(nameof(Handle));

    private void Throw() => throw new InvalidOperationException("boom");
    private ExceptionDirective Handle(ExceptionContext<EPState, EPTrigger> ctx) => ExceptionDirective.Continue;
}

public enum EPState { A, B }
public enum EPTrigger { Go }
