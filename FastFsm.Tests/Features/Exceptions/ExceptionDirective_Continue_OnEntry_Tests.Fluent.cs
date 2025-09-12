using System;
using System.Collections.Generic;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Continue_OnEntry_Tests
{
    [Fact]
    public void OnEntryThrow_Continue_SwallowsAndContinues_StateChanged_ActionRuns()
    {
        var m = new ContinueOnEntryMachineFluent(EDState.A) { ThrowOnEntryB = true };
        m.Start();

        Assert.Equal(EDState.A, m.CurrentState);

        m.Fire(EDTrigger.Go);

        Assert.Equal(EDState.B, m.CurrentState);
        Assert.Equal(new[] { "OnEntryB-THREW", "Action-A->B" }, m.Log);
    }
}

public enum EDState { A, B }
public enum EDTrigger { Go }

[StateMachine(typeof(EDState), typeof(EDTrigger))]
public partial class ContinueOnEntryMachineFluent
{
    public List<string> Log { get; } = new();
    public bool ThrowOnEntryB { get; set; }

    private static void Configure() => FSM
        .OnException<EDState>(nameof(Handle))
        .State(EDState.A)
            .On(EDTrigger.Go).Action(nameof(ActionAB)).GoTo(EDState.B)
        .State(EDState.B)
            .OnEntry(nameof(OnEntryB));

    private void OnEntryB()
    {
        if (ThrowOnEntryB)
        {
            Log.Add("OnEntryB-THREW");
            throw new TransientDeviceException("transient");
        }
        Log.Add("OnEntryB-OK");
    }

    private void ActionAB() => Log.Add("Action-A->B");

    private ExceptionDirective Handle(ExceptionContext<EDState, EDTrigger> ctx)
        => ctx.Exception is TransientDeviceException
            ? ExceptionDirective.Continue
            : ExceptionDirective.Propagate;
}

public sealed class TransientDeviceException : Exception
{
    public TransientDeviceException(string message) : base(message) { }
}