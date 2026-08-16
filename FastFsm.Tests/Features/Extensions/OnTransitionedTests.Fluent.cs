using System.Collections.Generic;
using Abstractions.Fluent;
using FastFsm.Contracts;
using Xunit;

namespace FastFsm.Tests.Features.Extensions;

public class OnTransitionedTestsFluent
{
    private sealed class TransitionedRecordingExtension : IStateMachineExtension
    {
        public int TransitionedCount { get; private set; }
        public List<string> Log { get; } = new();

        public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext => Log.Add("Before");
        public void OnAfterTransition<T>(T ctx, bool s) where T : IStateMachineContext => Log.Add($"After:{(s ? "Success" : "Fail")}");
        public void OnGuardEvaluation<T>(T ctx, string g) where T : IStateMachineContext => Log.Add("GuardEval");
        public void OnGuardEvaluated<T>(T ctx, string g, bool r) where T : IStateMachineContext => Log.Add("GuardEvaluated");
        public void OnUnhandledTrigger<T>(T ctx) where T : IStateMachineContext { }
        public void OnInternalTransition<T>(T ctx) where T : IStateMachineContext { }
        public void OnTransitioned<T>(T ctx) where T : IStateMachineContext { TransitionedCount++; Log.Add("Transitioned"); }
    }

    [Fact]
    public void Fluent_Transitioned_FiresOnce_InOrder()
    {
        var ext = new TransitionedRecordingExtension();
        var m = new TransitionedMachineFluent(Tr2State.A, new IStateMachineExtension[] { ext });
        m.Start();

        var ok = m.TryFire(Tr2Trigger.Go);

        Assert.True(ok);
        Assert.Equal(Tr2State.B, m.CurrentState);
        Assert.Equal(1, ext.TransitionedCount);
        Assert.Collection(ext.Log,
            s => Assert.Equal("Before", s),
            s => Assert.Equal("Transitioned", s),
            s => Assert.Equal("After:Success", s)
        );
    }
}

[StateMachine(typeof(Tr2State), typeof(Tr2Trigger), GenerateExtensibleVersion = true)]
public partial class TransitionedMachineFluent
{
    private static void Configure() => FSM
        .State(Tr2State.A)
            .On(Tr2Trigger.Go).GoTo(Tr2State.B)
        .State(Tr2State.B);
}

public enum Tr2State { A, B }
public enum Tr2Trigger { Go }
