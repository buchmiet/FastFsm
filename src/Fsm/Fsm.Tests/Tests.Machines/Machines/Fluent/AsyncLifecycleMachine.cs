using Abstractions.Fluent;
using Abstractions.Attributes;
using System;
using System.Threading.Tasks;

namespace Tests.Machines.Machines.Fluent;


[StateMachine(typeof(AsyncLifecycleState), typeof(AsyncLifecycleTrigger))]
public partial class AsyncLifecycleMachine
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

    private async Task StartProcessingAsync() => await Task.Delay(10); // Simulate async work

    private async Task FailingActionAsync()
    {
        await Task.Delay(5);
        throw new InvalidOperationException("Async action failed");
    }
}