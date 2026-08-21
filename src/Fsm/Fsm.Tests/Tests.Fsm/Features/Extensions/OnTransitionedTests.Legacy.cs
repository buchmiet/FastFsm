using System.Collections.Generic;
using Abstractions.Attributes;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Features.Extensions;

public class OnTransitionedTestsLegacy
{
    private sealed class TransitionedRecordingExtension : IStateMachineExtension<TrState, TrTrigger>
    {
        public int TransitionedCount { get; private set; }
        public int AfterTrueCount { get; private set; }
        public List<string> Log { get; } = new();

        public void OnAttemptStarting(in TransitionAttemptContext<TrState, TrTrigger> attempt) => Log.Add("Before");
        public void OnAttemptCompleted(
            in TransitionAttemptContext<TrState, TrTrigger> attempt,
            in TransitionResult<TrState> result)
        {
            if (result.Outcome == TransitionOutcome.Succeeded) TransitionedCount++;
            Log.Add($"After:{(result.Outcome == TransitionOutcome.Succeeded ? "Success" : "Fail")}");
        }
    }

    [Fact]
    public void Legacy_Transitioned_FiresOnce_InOrder()
    {
        var ext = new TransitionedRecordingExtension();
        var m = new TransitionedMachineLegacy(TrState.A, [ext]);
        m.Start();

        var ok = m.TryFire(TrTrigger.Go);

        Assert.True(ok);
        Assert.Equal(TrState.B, m.CurrentState);
        Assert.Equal(1, ext.TransitionedCount);
        Assert.Collection(ext.Log,
            s => Assert.Equal("Before", s),
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
