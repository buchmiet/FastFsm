using Abstractions.Fluent;
using Abstractions.Attributes;
using System.Threading.Tasks;

namespace FastFsm.Logging.Tests;

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

// Async lifecycle state machine - Fluent version
[StateMachine(typeof(AsyncLifecycleState), typeof(AsyncLifecycleTrigger))]
public partial class AsyncLifecycleMachineFluent
{
    private void Configure() => FSM
        .State(AsyncLifecycleState.Initial)
            .On(AsyncLifecycleTrigger.StartAsync)
                .Action(nameof(StartProcessingAsync))
                .GoTo(AsyncLifecycleState.Processing)
        .State(AsyncLifecycleState.Processing)
            .On(AsyncLifecycleTrigger.FailAsync)
                .Action(nameof(FailingActionAsync))
                .GoTo(AsyncLifecycleState.Failed)
            .On(AsyncLifecycleTrigger.CompleteAsync)
                .GoTo(AsyncLifecycleState.Completed);

    private async Task StartProcessingAsync()
    {
        await Task.Delay(10);
    }

    private async Task FailingActionAsync()
    {
        await Task.Delay(5);
        throw new InvalidOperationException("Async action failed");
    }
}
