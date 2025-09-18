using Abstractions.Fluent;
using FastFsm.Contracts;
using Xunit;

namespace FastFsm.Tests.Features.Extensions;

public class OnInternalTransitionTestsFluent
{
    private sealed class InternalRecordingExtension : IStateMachineExtension
    {
        public int InternalCount { get; private set; }
        public int AfterTrueCount { get; private set; }

        public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
        {
            InternalCount++;
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            if (success) AfterTrueCount++;
        }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext { }
    }

    [Fact]
    public void Fluent_InternalTransition_Fires_Hook()
    {
        var ext = new InternalRecordingExtension();
        var m = new InternalMachineFluent(IntState2.A, new IStateMachineExtension[] { ext });
        m.Start();

        var ok = m.TryFire(IntTrigger2.Ping);

        Assert.True(ok);
        Assert.Equal(IntState2.A, m.CurrentState);
        Assert.Equal(1, ext.InternalCount);
        Assert.Equal(1, ext.AfterTrueCount);
    }
}

[StateMachine(typeof(IntState2), typeof(IntTrigger2), GenerateExtensibleVersion = true)]
public partial class InternalMachineFluent
{
    private static void Configure() => FSM
        .State(IntState2.A)
            .OnInternal(IntTrigger2.Ping)
                .Action(nameof(Ping))
                .Internal();

    private void Ping() { }
}

public enum IntState2 { A }
public enum IntTrigger2 { Ping }

