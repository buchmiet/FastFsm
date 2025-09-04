using System;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    public enum AaslState { Initial, Processing, Completed }
    public enum AaslTrigger { Start, Process, Complete }

    // Legacy attribute-based version (as in FastFsm.Async.Tests SimpleAsyncMachine)
    [StateMachine(typeof(AaslState), typeof(AaslTrigger))]
    public partial class AsyncSimpleLegacyStateMachine
    {
        [Transition(AaslState.Initial, AaslTrigger.Start, AaslState.Processing, Guard = nameof(CanStartAsync))]
        private async ValueTask<bool> CanStartAsync()
        {
            await Task.Delay(1);
            return true;
        }

        [Transition(AaslState.Processing, AaslTrigger.Process, AaslState.Processing, Action = nameof(ProcessAsync))]
        private async Task ProcessAsync()
        {
            await Task.Delay(1);
        }

        [Transition(AaslState.Processing, AaslTrigger.Complete, AaslState.Completed, Action = nameof(Complete))]
        private void Complete() { }

        [State(AaslState.Processing, OnEntry = nameof(OnProcessingEntryAsync))]
        private async Task OnProcessingEntryAsync() { await Task.Delay(1); }

        [State(AaslState.Processing, OnExit = nameof(OnProcessingExitAsync))]
        private async ValueTask OnProcessingExitAsync() { await Task.Delay(1); }
    }

    // Fluent version (aligned to legacy: Process is EXTERNAL self-loop)
    [StateMachine(typeof(AaslState), typeof(AaslTrigger))]
    public partial class AsyncSimpleFluentMachine
    {
        private static void Configure() => FSM
            .State(AaslState.Initial)
                .On(AaslTrigger.Start)
                    .GuardAsync(nameof(CanStartAsync))
                    .GoTo(AaslState.Processing)
            .State(AaslState.Processing)
                .OnEntryAsync(nameof(OnProcessingEntryAsync))
                .OnExitAsync(nameof(OnProcessingExitAsync))
                .On(AaslTrigger.Process)
                    .ActionAsync(nameof(ProcessAsync))
                    .GoTo(AaslState.Processing)
                .On(AaslTrigger.Complete)
                    .Action(nameof(Complete))
                    .GoTo(AaslState.Completed)
            .State(AaslState.Completed);

        private async ValueTask<bool> CanStartAsync() { await Task.Delay(1); return true; }
        private async Task ProcessAsync() { await Task.Delay(1); }
        private void Complete() { }
        private async Task OnProcessingEntryAsync() { await Task.Delay(1); }
        private async ValueTask OnProcessingExitAsync() { await Task.Delay(1); }
    }
}
