using System;
using System.Threading.Tasks;
using Abstractions.Attributes;

namespace StateMachine.Async.Tests.Machines;

[StateMachine(typeof(AsyncState), typeof(AsyncTrigger))]
public partial class AsyncExceptionMachine
{
    [Transition(AsyncState.A, AsyncTrigger.Go, AsyncState.B, Action = nameof(ThrowingActionAsync))]
    private void Configure() { }

    private async Task ThrowingActionAsync()
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Async action failed!");
    }
}