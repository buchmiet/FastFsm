using FastFsm.Contracts;
using Xunit;

namespace FastFsm.Tests.Features.Extensions;

public class OnInternalTransitionTestsLegacy
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
    public void Legacy_InternalTransition_Fires_Hook()
    {
        var ext = new InternalRecordingExtension();
        var m = new InternalMachineLegacy(IntState.A, new IStateMachineExtension[] { ext });
        m.Start();

        var ok = m.TryFire(IntTrigger.Ping);

        Assert.True(ok);
        Assert.Equal(IntState.A, m.CurrentState); // no state change
        Assert.Equal(1, ext.InternalCount);
        Assert.Equal(1, ext.AfterTrueCount);
    }
}

[StateMachine(typeof(IntState), typeof(IntTrigger), GenerateExtensibleVersion = true)]
public partial class InternalMachineLegacy
{
    [InternalTransition(IntState.A, IntTrigger.Ping, Action = nameof(Ping))]
    private void Configure() { }

    private void Ping() { }
}

public enum IntState { A }
public enum IntTrigger { Ping }

