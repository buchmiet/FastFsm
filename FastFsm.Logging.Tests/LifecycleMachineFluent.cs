using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Logging.Tests;

[StateMachine(typeof(LifecycleState), typeof(LifecycleTrigger))]
public partial class LifecycleMachineFluent
{
    private static void Configure() => FSM
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