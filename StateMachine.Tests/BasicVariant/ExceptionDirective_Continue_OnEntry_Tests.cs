//using System;
//using System.Collections.Generic;
//using Abstractions.Attributes;
//using StateMachine.Exceptions;
//using Xunit;

//namespace StateMachine.Tests.BasicVariant;

//public class ExceptionDirective_Continue_OnEntry_Tests
//{
//    [Fact]
//    public void OnEntryThrow_Continue_SwallowsAndContinues_StateChanged_ActionRuns()
//    {
//        var m = new ContinueOnEntryMachine(EDState.A) { ThrowOnEntryB = true };

//        Assert.Equal(EDState.A, m.CurrentState);

//        m.Fire(EDTrigger.Go);

//        Assert.Equal(EDState.B, m.CurrentState);
//        Assert.Equal(new[] { "OnEntryB-THREW", "Action-A->B" }, m.Log);
//    }
//}

//public enum EDState { A, B }
//public enum EDTrigger { Go }

//[StateMachine(typeof(EDState), typeof(EDTrigger))]
//[OnException(nameof(Handle))]
//public partial class ContinueOnEntryMachine
//{
//    public List<string> Log { get; } = new();
//    public bool ThrowOnEntryB { get; set; }

//    [State(EDState.B, OnEntry = nameof(OnEntryB))]
//    [Transition(EDState.A, EDTrigger.Go, EDState.B, Action = nameof(ActionAB))]
//    private void Configure() { }

//    private void OnEntryB()
//    {
//        if (ThrowOnEntryB)
//        {
//            Log.Add("OnEntryB-THREW");
//            throw new TransientDeviceException("transient");
//        }
//        Log.Add("OnEntryB-OK");
//    }

//    private void ActionAB() => Log.Add("Action-A->B");

//    private ExceptionDirective Handle(ExceptionContext<EDState, EDTrigger> ctx)
//        => ctx.Exception is TransientDeviceException
//            ? ExceptionDirective.Continue
//            : ExceptionDirective.Propagate;
//}

//public sealed class TransientDeviceException : Exception
//{
//    public TransientDeviceException(string message) : base(message) { }
//}