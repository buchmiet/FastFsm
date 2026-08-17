using System.Collections.Generic;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Features.Extensions;

public class OnTransitionedTestsLegacy
{
    private sealed class TransitionedRecordingExtension : IStateMachineExtension
    {
        public int TransitionedCount { get; private set; }
        public int AfterTrueCount { get; private set; }
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
    public void Legacy_Transitioned_FiresOnce_InOrder()
    {
        var ext = new TransitionedRecordingExtension();
        var m = new TransitionedMachineLegacy(TrState.A, new IStateMachineExtension[] { ext });
        m.Start();

        var ok = m.TryFire(TrTrigger.Go);

        Assert.True(ok);
        Assert.Equal(TrState.B, m.CurrentState);
        Assert.Equal(1, ext.TransitionedCount);
        Assert.Collection(ext.Log,
            s => Assert.Equal("Before", s),
            s => Assert.Equal("Transitioned", s),
            s => Assert.Equal("After:Success", s)
        );
    }
}

[StateMachine(typeof(TrState), typeof(TrTrigger), GenerateExtensibleVersion = true)]
public partial class TransitionedMachineLegacy
{
    [Transition(TrState.A, TrTrigger.Go, TrState.B)]
    private void Configure() { }
}

public enum TrState { A, B }
public enum TrTrigger { Go }
