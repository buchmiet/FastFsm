using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace Tests.Async.Features.Concurrency;
    public enum RcStates { Initial, A, B }
    public enum RcTriggers { ToA, ToB }

    [StateMachine(typeof(RcStates), typeof(RcTriggers))]
    public partial class RcMachine
    {
        // Liczniki do asercji (atomowe)
        private int _onExitCalls;
        private int _onEntryACalls;
        private int _onEntryBCalls;

        public int OnExitCalls => Volatile.Read(ref _onExitCalls);
        public int OnEntryACalls => Volatile.Read(ref _onEntryACalls);
        public int OnEntryBCalls => Volatile.Read(ref _onEntryBCalls);

        // --- Concurrency probe (barrier) ---
        private static int _slowActionEntered;
        private static TaskCompletionSource<bool> _firstInside =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static TaskCompletionSource<bool> _releaseFirst =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SlowActionCalls => Volatile.Read(ref _slowActionEntered);

        public static void ResetConcurrencyProbe()
        {
            Interlocked.Exchange(ref _slowActionEntered, 0);
            _firstInside = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public static async Task WaitUntilFirstInsideAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await _firstInside.Task.WaitAsync(cts.Token);
        }

        public static void ReleaseFirst() => _releaseFirst.TrySetResult(true);

        // ----- State hooks and attributed action -----

        [State(RcStates.Initial, OnExit = nameof(OnInitialExitAsync))]
        private async Task OnInitialExitAsync()
        {
            Interlocked.Increment(ref _onExitCalls);
            await Task.Yield();
        }

        [Transition(RcStates.Initial, RcTriggers.ToA, RcStates.A, Action = nameof(SlowActionAsync))]
        [Transition(RcStates.Initial, RcTriggers.ToB, RcStates.B, Action = nameof(SlowActionAsync))]
        private async Task SlowActionAsync()
        {
            var n = Interlocked.Increment(ref _slowActionEntered);
            if (n == 1)
                _firstInside.TrySetResult(true);       // signal: first caller entered

            // First call waits for "release"; the second cannot enter
            // until the first finishes (serialized by SemaphoreSlim on the base).
            await _releaseFirst.Task.ConfigureAwait(false);
        }

        [State(RcStates.A, OnEntry = nameof(OnEntryAAsync))]
        private async Task OnEntryAAsync()
        {
            Interlocked.Increment(ref _onEntryACalls);
            await Task.Yield();
        }

        [State(RcStates.B, OnEntry = nameof(OnEntryBAsync))]
        private async Task OnEntryBAsync()
        {
            Interlocked.Increment(ref _onEntryBCalls);
            await Task.Yield();
        }
    }
 
    // Fluent API equivalent
    [StateMachine(typeof(RcStates), typeof(RcTriggers))]
    public partial class RcMachineFluentFsm
    {
        // Liczniki do asercji (atomowe)
        private int _onExitCalls;
        private int _onEntryACalls;
        private int _onEntryBCalls;

        public int OnExitCalls => Volatile.Read(ref _onExitCalls);
        public int OnEntryACalls => Volatile.Read(ref _onEntryACalls);
        public int OnEntryBCalls => Volatile.Read(ref _onEntryBCalls);

        // --- Concurrency probe (barrier) ---
        private static int _slowActionEntered;
        private static TaskCompletionSource<bool> _firstInside =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static TaskCompletionSource<bool> _releaseFirst =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SlowActionCalls => Volatile.Read(ref _slowActionEntered);

        public static void ResetConcurrencyProbe()
        {
            Interlocked.Exchange(ref _slowActionEntered, 0);
            _firstInside = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public static async Task WaitUntilFirstInsideAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            await _firstInside.Task.WaitAsync(cts.Token);
        }

        public static void ReleaseFirst() => _releaseFirst.TrySetResult(true);

        private void Configure() => FSM
            .State(RcStates.Initial)
                .OnExitAsync(nameof(OnInitialExitAsync))
                .On(RcTriggers.ToA).Action(nameof(SlowActionAsync)).GoTo(RcStates.A)
                .On(RcTriggers.ToB).Action(nameof(SlowActionAsync)).GoTo(RcStates.B)
            .State(RcStates.A)
                .OnEntryAsync(nameof(OnEntryAAsync))
            .State(RcStates.B)
                .OnEntryAsync(nameof(OnEntryBAsync));

        private async Task OnInitialExitAsync()
        {
            Interlocked.Increment(ref _onExitCalls);
            await Task.Yield();
        }

        private async Task SlowActionAsync()
        {
            var n = Interlocked.Increment(ref _slowActionEntered);
            if (n == 1)
                _firstInside.TrySetResult(true);
            await _releaseFirst.Task.ConfigureAwait(false);
        }

        private async Task OnEntryAAsync()
        {
            Interlocked.Increment(ref _onEntryACalls);
            await Task.Yield();
        }

        private async Task OnEntryBAsync()
        {
            Interlocked.Increment(ref _onEntryBCalls);
            await Task.Yield();
        }
    }
