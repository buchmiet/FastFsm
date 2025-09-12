using Abstractions.Fluent;
using Abstractions.Attributes;
using System;
using System.Threading.Tasks;

namespace FastFsm.Logging.Tests;

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

// Async lifecycle state machine - Fluent version
[StateMachine(typeof(AsyncLifecycleState), typeof(AsyncLifecycleTrigger))]
public partial class AsyncLifecycleMachineFluent
{
    private static void Configure() => FSM
        .State(AsyncLifecycleState.Initial)
            .On(AsyncLifecycleTrigger.StartAsync)
                .Action(nameof(StartProcessingAsync))
                .GoTo(AsyncLifecycleState.Processing).And()
        .State(AsyncLifecycleState.Processing)
            .On(AsyncLifecycleTrigger.FailAsync)
                .Action(nameof(FailingActionAsync))
                .GoTo(AsyncLifecycleState.Failed).And()
            .On(AsyncLifecycleTrigger.CompleteAsync)
                .GoTo(AsyncLifecycleState.Completed);

    private async Task StartProcessingAsync()
    {
        await Task.Delay(10); // Simulate async work
    }

    private async Task FailingActionAsync()
    {
        await Task.Delay(5);
        throw new InvalidOperationException("Async action failed");
    }
}