using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;

namespace StateMachine.Async.Tests.Machines;

[StateMachine(typeof(AsyncState), typeof(AsyncTrigger))]
public partial class AsyncCancellableMachine
{
    [Transition(AsyncState.A, AsyncTrigger.Go, AsyncState.B, Action = nameof(LongRunningActionAsync))]
    private void Configure() { }

    // Ważne: metoda musi przyjmować CancellationToken
    public async Task LongRunningActionAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(5000, cancellationToken);
    }
}