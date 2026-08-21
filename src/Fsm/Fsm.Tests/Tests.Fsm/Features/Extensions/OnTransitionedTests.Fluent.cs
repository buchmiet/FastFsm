using System.Collections.Generic;
using Abstractions.Fluent;
using FastFsm.Contracts;
using Xunit;

namespace Tests.Fsm.Features.Extensions;

public class OnTransitionedTestsFluent
{
    private sealed class TransitionedRecordingExtension : IStateMachineExtension<Tr2State, Tr2Trigger>
    {
        public int TransitionedCount { get; private set; }
        public List<string> Log { get; } = new();

        public void OnAttemptStarting(in TransitionAttemptContext<Tr2State, Tr2Trigger> attempt) => Log.Add("Before");
        public void OnAttemptCompleted(
            in TransitionAttemptContext<Tr2State, Tr2Trigger> attempt,
            in TransitionResult<Tr2State> result)
        {
            if (result.Outcome == TransitionOutcome.Succeeded) TransitionedCount++;
            Log.Add($"After:{(result.Outcome == TransitionOutcome.Succeeded ? "Success" : "Fail")}");
        }
    }

    [Fact]
    public void Fluent_Transitioned_FiresOnce_InOrder()
    {
        var ext = new TransitionedRecordingExtension();
        var m = new TransitionedMachineFluent(Tr2State.A, [ext]);
        m.Start();

        var ok = m.TryFire(Tr2Trigger.Go);

        Assert.True(ok);
        Assert.Equal(Tr2State.B, m.CurrentState);
        Assert.Equal(1, ext.TransitionedCount);
        Assert.Collection(ext.Log,
            s => Assert.Equal("Before", s),
            s => Assert.Equal("After:Success", s)
        );
    }
}

[StateMachine(typeof(Tr2State), typeof(Tr2Trigger), GenerateExtensibleVersion = true)]
public partial class TransitionedMachineFluent
{
    private void Configure() => FSM
        .State(Tr2State.A)
            .On(Tr2Trigger.Go).GoTo(Tr2State.B)
        .State(Tr2State.B);
}

public enum Tr2State { A, B }
public enum Tr2Trigger { Go }
