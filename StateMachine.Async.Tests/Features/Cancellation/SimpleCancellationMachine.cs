using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;

namespace StateMachine.Async.Tests.Features.Cancellation
{
    // Simple machine to test cancellation token propagation
    [StateMachine(typeof(SimpleStates), typeof(SimpleTriggers))]
    public partial class SimpleCancellationMachine
    {
        [State(SimpleStates.Ready, OnEntry = nameof(OnEnterReady))]
        [State(SimpleStates.Working)]
        [State(SimpleStates.Done)]
        private void ConfigureStates() { }

        [Transition(SimpleStates.Ready, SimpleTriggers.Start, SimpleStates.Working,
            Guard = nameof(CanStart), Action = nameof(DoStart))]
        [Transition(SimpleStates.Working, SimpleTriggers.Finish, SimpleStates.Done)]
        private void ConfigureTransitions() { }

        // All callbacks accept CancellationToken with default value
        private async ValueTask<bool> CanStart(CancellationToken ct = default)
        {
            await Task.Delay(1, ct);
            return true;
        }

        private async Task DoStart(CancellationToken ct = default)
        {
            await Task.Delay(1, ct);
        }

        private async Task OnEnterReady(CancellationToken ct = default)
        {
            await Task.Delay(1, ct);
        }
    }

    public enum SimpleStates
    {
        Ready,
        Working,
        Done
    }

    public enum SimpleTriggers
    {
        Start,
        Finish
    }
}
