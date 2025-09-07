using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Continue_OnEntry_Tests_Fluent
{
    [Fact]
    public void OnEntryThrow_Continue_SwallowsAndContinues_StateChanged_ActionRuns()
    {
        var m = new ContinueOnEntryMachine_Fluent(EDState_Fluent.A) { ThrowOnEntryB = true };

        Assert.Equal(EDState_Fluent.A, m.CurrentState);

        m.Fire(EDTrigger_Fluent.Go);

        Assert.Equal(EDState_Fluent.B, m.CurrentState);
        Assert.Equal(new[] { "OnEntryB-THREW", "Action-A->B" }, m.Log);
    }
}

public enum EDState_Fluent { A, B }
public enum EDTrigger_Fluent { Go }

[StateMachine(typeof(EDState_Fluent), typeof(EDTrigger_Fluent))]
public partial class ContinueOnEntryMachine_Fluent
{
    public List<string> Log { get; } = new();
    public bool ThrowOnEntryB { get; set; }

    private static void Configure() => FSM
        .OnException<EDState_Fluent>(nameof(Handle))
        .State(EDState_Fluent.A)
            .On(EDTrigger_Fluent.Go).GoTo(EDState_Fluent.B).Do(nameof(ActionAB))
        .State(EDState_Fluent.B)
            .OnEntry(nameof(OnEntryB));

    private void OnEntryB()
    {
        if (ThrowOnEntryB)
        {
            Log.Add("OnEntryB-THREW");
            throw new TransientDeviceException_Fluent("transient");
        }
        Log.Add("OnEntryB-OK");
    }

    private void ActionAB() => Log.Add("Action-A->B");

    private ExceptionDirective Handle(ExceptionContext<EDState_Fluent, EDTrigger_Fluent> ctx)
        => ctx.Exception is TransientDeviceException_Fluent
            ? ExceptionDirective.Continue
            : ExceptionDirective.Propagate;
}

public sealed class TransientDeviceException_Fluent : Exception
{
    public TransientDeviceException_Fluent(string message) : base(message) { }
}