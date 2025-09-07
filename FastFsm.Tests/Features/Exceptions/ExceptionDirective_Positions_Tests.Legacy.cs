using System;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

public class ExceptionDirective_Positions_TestsLegacy
{
    [Fact]
    public void OnException_InMiddle_Continue_Swallows()
    {
        var m = new MiddlePositionMachineLegacy(MPStateLegacy.A);
        m.Start();
        m.Fire(MPTriggerLegacy.Go);
        Assert.Equal(MPStateLegacy.B, m.CurrentState);
    }

    [Fact]
    public void OnException_AtEnd_Continue_Swallows()
    {
        var m = new EndPositionMachineLegacy(EPStateLegacy.A);
        m.Start();
        m.Fire(EPTriggerLegacy.Go);
        Assert.Equal(EPStateLegacy.B, m.CurrentState);
    }
}

[StateMachine(typeof(MPStateLegacy), typeof(MPTriggerLegacy))]
[OnException(nameof(Handle))]
public partial class MiddlePositionMachineLegacy
{
    [Transition(MPStateLegacy.A, MPTriggerLegacy.Go, MPStateLegacy.B, Action = nameof(Throw))]
    private void Throw() => throw new InvalidOperationException("boom");
    
    private ExceptionDirective Handle(ExceptionContext<MPStateLegacy, MPTriggerLegacy> ctx) => ExceptionDirective.Continue;
}

public enum MPStateLegacy { A, B }
public enum MPTriggerLegacy { Go }

[StateMachine(typeof(EPStateLegacy), typeof(EPTriggerLegacy))]
[OnException(nameof(Handle))]
public partial class EndPositionMachineLegacy
{
    [Transition(EPStateLegacy.A, EPTriggerLegacy.Go, EPStateLegacy.B, Action = nameof(Throw))]
    private void Throw() => throw new InvalidOperationException("boom");
    
    private ExceptionDirective Handle(ExceptionContext<EPStateLegacy, EPTriggerLegacy> ctx) => ExceptionDirective.Continue;
}

public enum EPStateLegacy { A, B }
public enum EPTriggerLegacy { Go }