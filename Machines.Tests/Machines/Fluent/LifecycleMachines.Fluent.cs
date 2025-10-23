using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

// Lifecycle state machine - Fluent version
[StateMachine(typeof(LifecycleState), typeof(LifecycleTrigger))]
public partial class LifecycleMachineFluent
{
    private static void Configure() => FSM
        .State(LifecycleState.Failed)
            .OnEntry(nameof(ThrowingEntry)).And()
        .State(LifecycleState.Initial)
            .On(LifecycleTrigger.Start).GoTo(LifecycleState.Processing).And()
        .State(LifecycleState.Processing)
            .On(LifecycleTrigger.Complete).GoTo(LifecycleState.Completed).And()
            .On(LifecycleTrigger.Fail).GoTo(LifecycleState.Failed);

    private void ThrowingEntry()
    {
        throw new InvalidOperationException("Entry callback failed");
    }
}
