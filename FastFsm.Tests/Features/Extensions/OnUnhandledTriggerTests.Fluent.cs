using System.Collections.Generic;
using FastFsm.Contracts;
using Xunit;

namespace FastFsm.Tests.Features.Extensions;

public class OnUnhandledTriggerTestsFluent
{
    private sealed class UnhandledRecordingExtension : IStateMachineExtension
    {
        public int UnhandledCount { get; private set; }
        public int AfterFalseCount { get; private set; }
        public readonly List<(object From, object Trigger, object To)> Snapshots = new();

        public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
        {
            UnhandledCount++;
            if (context is IStateSnapshot s)
            {
                Snapshots.Add((s.FromState, s.Trigger, s.ToState));
            }
        }

        public void OnAfterTransition<TContext>(TContext context, bool success) where TContext : IStateMachineContext
        {
            if (!success) AfterFalseCount++;
        }

        public void OnBeforeTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
        public void OnGuardEvaluation<TContext>(TContext context, string guardName) where TContext : IStateMachineContext { }
        public void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) where TContext : IStateMachineContext { }
        public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext { }
    }

    [Fact]
    public void Fluent_Unhandled_Fires_OnUnhandledTrigger()
    {
        // Arrange: z Idle nie ma przejścia po Cancel
        var ext = new UnhandledRecordingExtension();
        var machine = new ExtensionsMachineFluent(ExtState.Idle, new IStateMachineExtension[] { ext });
        machine.Start();

        // Act
        var ok = machine.TryFire(ExtTrigger.Cancel);

        // Assert
        Assert.False(ok);
        Assert.Equal(1, ext.UnhandledCount);
        Assert.Equal(1, ext.AfterFalseCount);

        Assert.Single(ext.Snapshots);
        var (from, trig, to) = ext.Snapshots[0];
        Assert.Equal(ExtState.Idle, from);
        Assert.Equal(ExtTrigger.Cancel, trig);
        Assert.Equal(ExtState.Idle, to); // no-transition case
    }
}
