using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

// Lifecycle state machine - Fluent version
[StateMachine(typeof(LifecycleState), typeof(LifecycleTrigger))]
public partial class LifecycleMachineFluent
{
    private void Configure() => FSM
        .State(LifecycleState.Failed)
            .OnEntry(nameof(ThrowingEntry))
        .State(LifecycleState.Initial)
            .On(LifecycleTrigger.Start).GoTo(LifecycleState.Processing)
        .State(LifecycleState.Processing)
            .On(LifecycleTrigger.Complete).GoTo(LifecycleState.Completed)
            .On(LifecycleTrigger.Fail).GoTo(LifecycleState.Failed);

    private void ThrowingEntry()
    {
        throw new InvalidOperationException("Entry callback failed");
    }
}
