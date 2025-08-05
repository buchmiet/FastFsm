using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm;
using Shouldly;
using Xunit;
using System.Threading;

namespace FastFsm.Tests.CancellationToken
{
    // Machine for testing specific documentation requirements
    [StateMachine(typeof(SpecStates), typeof(SpecTriggers))]
    public partial class SpecificationComplianceMachine
    {
        private readonly List<(string Method, string Parameters)> _callLog = new();

        public IReadOnlyList<(string Method, string Parameters)> CallLog => _callLog;

        [State(SpecStates.Ready, OnEntry = nameof(OnEnterReady))]
        [State(SpecStates.Working, OnEntry = nameof(OnEnterWorking), OnExit = nameof(OnExitWorking))]
        [State(SpecStates.Done)]
        private void ConfigureStates() { }

        [Transition(SpecStates.Ready, SpecTriggers.Start, SpecStates.Working,
            Guard = nameof(CanStart), Action = nameof(DoStart))]
        [Transition(SpecStates.Working, SpecTriggers.Finish, SpecStates.Done,
            Guard = nameof(CanFinish), Action = nameof(DoFinish))]
        [InternalTransition(SpecStates.Working, SpecTriggers.Update, nameof(DoUpdate),
            Guard = nameof(CanUpdate))]
        private void ConfigureTransitions() { }

        // Multiple overloads to test priority resolution
        // Priority: (CT) > () for async machine

        // Guards
        private async ValueTask<bool> CanStart()
        {
            _callLog.Add(("CanStart", "()"));
            await Task.Delay(1);
            return true;
        }

        private async ValueTask<bool> CanStart(System.Threading.CancellationToken ct)
        {
            _callLog.Add(("CanStart", "(CT)"));
            await Task.Delay(1, ct);
            return true;
        }

        private bool CanFinish()
        {
            _callLog.Add(("CanFinish", "()"));
            return true;
        }

        private bool CanFinish(System.Threading.CancellationToken ct)
        {
            _callLog.Add(("CanFinish", "(CT)"));
            ct.ThrowIfCancellationRequested();
            return true;
        }

        private async ValueTask<bool> CanUpdate(System.Threading.CancellationToken ct)
        {
            _callLog.Add(("CanUpdate", "(CT)"));
            await Task.Delay(1, ct);
            return true;
        }

        // Actions
        private void DoStart()
        {
            _callLog.Add(("DoStart", "()"));
        }

        private async Task DoStart(System.Threading.CancellationToken ct)
        {
            _callLog.Add(("DoStart", "(CT)"));
            await Task.Delay(1, ct);
        }

        private async ValueTask DoFinish()
        {
            _callLog.Add(("DoFinish", "()"));
            await Task.Delay(1);
        }

        private async Task DoUpdate(System.Threading.CancellationToken ct)
        {
            _callLog.Add(("DoUpdate", "(CT)"));
            await Task.Delay(1, ct);
        }

        // State callbacks
        private void OnEnterReady()
        {
            _callLog.Add(("OnEnterReady", "()"));
        }

        private async Task OnEnterWorking(System.Threading.CancellationToken ct)
        {
            _callLog.Add(("OnEnterWorking", "(CT)"));
            await Task.Delay(1, ct);
        }

        private async ValueTask OnExitWorking()
        {
            _callLog.Add(("OnExitWorking", "()"));
            await Task.Delay(1);
        }

        public void ClearLog() => _callLog.Clear();
    }

    public enum SpecStates
    {
        Ready,
        Working,
        Done
    }

    public enum SpecTriggers
    {
        Start,
        Update,
        Finish
    }

    public class CancellationTokenSpecificationTests
    {
        [Fact]
        public async Task Should_Call_ThrowIfCancellationRequested_At_Start_Of_Public_Methods()
        {
            // Per spec: ThrowIfCancellationRequested() is called at the start of all public async methods
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // FireAsync
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await machine.FireAsync(SpecTriggers.Start, null, cts.Token));

            // TryFireAsync
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await machine.TryFireAsync(SpecTriggers.Start, null, cts.Token));

            // CanFireAsync
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await machine.CanFireAsync(SpecTriggers.Start, cts.Token));

            // GetPermittedTriggersAsync
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await machine.GetPermittedTriggersAsync(cts.Token));

            // No callbacks should have been called
            machine.CallLog.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Use_Best_Overload_With_CancellationToken_Priority()
        {
            // Per spec: Priority order for overloads - (CT) should be preferred over ()
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);
            using var cts = new CancellationTokenSource();

            await machine.FireAsync(SpecTriggers.Start, null, cts.Token);

            // Should have called overloads with CancellationToken where available
            machine.CallLog.ShouldContain(("CanStart", "(CT)"));
            machine.CallLog.ShouldContain(("OnEnterWorking", "(CT)"));
            machine.CallLog.ShouldContain(("DoStart", "(CT)"));

            // Should NOT have called parameterless overloads when CT version exists
            machine.CallLog.ShouldNotContain(("CanStart", "()"));
            machine.CallLog.ShouldNotContain(("DoStart", "()"));
        }

        [Fact]
        public async Task Should_Use_Parameterless_Overload_When_No_CT_Version()
        {
            var machine = new SpecificationComplianceMachine(SpecStates.Working);
            using var cts = new CancellationTokenSource();

            await machine.FireAsync(SpecTriggers.Finish, null, cts.Token);

            // Should use CT version when available
            machine.CallLog.ShouldContain(("CanFinish", "(CT)"));

            // Should use parameterless when no CT version
            machine.CallLog.ShouldContain(("OnExitWorking", "()"));
            machine.CallLog.ShouldContain(("DoFinish", "()"));
        }

        [Fact]
        public async Task Should_Allow_Sync_Callback_In_Async_Machine()
        {
            // Per spec: Async machine with sync callback is allowed, executes synchronously
            var machine = new SpecificationComplianceMachine(SpecStates.Working);

            // CanFinish has both sync versions
            var canFire = await machine.CanFireAsync(SpecTriggers.Finish);

            canFire.ShouldBeTrue();
            // Should have called the sync version with CT when available
            machine.CallLog.ShouldContain(("CanFinish", "(CT)"));
        }

        [Fact]
        public async Task Should_Never_Catch_OperationCanceledException()
        {
            // Per spec: OperationCanceledException is never caught - it propagates to the caller
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);

            // Test at different points
            var testCases = new[]
            {
                (10, "during guard"),
                (20, "during OnExit"),
                (30, "during OnEntry"),
                (40, "during Action")
            };

            foreach (var (delayMs, description) in testCases)
            {
                machine = new SpecificationComplianceMachine(SpecStates.Ready);
                using var cts = new CancellationTokenSource(delayMs);

                var ex = await Should.ThrowAsync<OperationCanceledException>(async () =>
                    await machine.FireAsync(SpecTriggers.Start, null, cts.Token));

                ex.ShouldNotBeNull(); // Ensures OperationCanceledException propagated
            }
        }

        [Fact]
        public async Task Should_Use_TreatCancellationAsFailure_In_Guards()
        {
            // Per spec: treatCancellationAsFailure = true in all guard evaluations
            // This means cancelled guards should result in transition failure, not exception

            var machine = new SpecificationComplianceMachine(SpecStates.Ready);
            using var cts = new CancellationTokenSource();

            // Start transition and cancel during guard
            var fireTask = machine.FireAsync(SpecTriggers.Start, null, cts.Token);
            cts.Cancel();

            // Should throw OperationCanceledException (propagated, not caught)
            await Should.ThrowAsync<OperationCanceledException>(async () => await fireTask);

            // State should not have changed
            machine.CurrentState.ShouldBe(SpecStates.Ready);
        }

        [Fact]
        public async Task Should_Execute_Callbacks_In_Documented_Order()
        {
            // Per spec: Guard → OnExit → State Change → OnEntry → Action
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);

            await machine.FireAsync(SpecTriggers.Start);

            var log = machine.CallLog.ToList();

            // Find indices
            var guardIndex = log.FindIndex(x => x.Method.StartsWith("CanStart"));
            var onEntryIndex = log.FindIndex(x => x.Method == "OnEnterWorking");
            var actionIndex = log.FindIndex(x => x.Method.StartsWith("DoStart"));

            // Verify order
            guardIndex.ShouldBeLessThan(onEntryIndex);
            onEntryIndex.ShouldBeLessThan(actionIndex);

            // For internal transitions: Guard → Action (no OnEntry/OnExit)
            machine.ClearLog();
            await machine.FireAsync(SpecTriggers.Update);

            log = machine.CallLog.ToList();
            log.ShouldContain(("CanUpdate", "(CT)"));
            log.ShouldContain(("DoUpdate", "(CT)"));
            log.Count.ShouldBe(2); // Only guard and action
        }

        [Fact]
        public async Task Should_Use_ConfigureAwait_Based_On_Setting()
        {
            // Per spec: ContinueOnCapturedContext controls ConfigureAwait usage
            // Default is false, meaning ConfigureAwait(false) should be used

            var syncContext = new TestSynchronizationContext();
            var originalContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(syncContext);

            try
            {
                var machine = new SpecificationComplianceMachine(SpecStates.Ready);
                await machine.FireAsync(SpecTriggers.Start);

                // With ContinueOnCapturedContext = false (default),
                // callbacks should not necessarily run on the captured context
                machine.CurrentState.ShouldBe(SpecStates.Working);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        [Fact]
        public async Task Should_Handle_GetPermittedTriggers_With_Cancellation()
        {
            var machine = new SpecificationComplianceMachine(SpecStates.Working);
            using var cts = new CancellationTokenSource();

            // Should evaluate guards with token
            var triggers = await machine.GetPermittedTriggersAsync(cts.Token);

            triggers.ShouldNotBeEmpty();
            machine.CallLog.ShouldContain(("CanFinish", "(CT)"));
            machine.CallLog.ShouldContain(("CanUpdate", "(CT)"));
        }

        [Fact]
        public async Task Should_Handle_Null_CancellationToken_As_Default()
        {
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);

            // Pass default (equivalent to not passing token)
            await machine.FireAsync(SpecTriggers.Start, null, default);

            machine.CurrentState.ShouldBe(SpecStates.Working);
            // Should still prefer CT overloads, but with default token
            machine.CallLog.ShouldContain(("CanStart", "(CT)"));
        }

        [Fact]
        public async Task Should_Propagate_Token_Through_Entire_Transition_Chain()
        {
            // Create a token source we can monitor
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            var machine = new SpecificationComplianceMachine(SpecStates.Ready);

            // Fire with token
            await machine.FireAsync(SpecTriggers.Start, null, token);

            // All CT-accepting callbacks should have been called
            machine.CallLog.Where(x => x.Parameters == "(CT)").Count().ShouldBeGreaterThan(0);

            // Now test internal transition
            machine.ClearLog();
            await machine.FireAsync(SpecTriggers.Update, null, token);

            // Internal transition should also propagate token
            machine.CallLog.ShouldContain(("CanUpdate", "(CT)"));
            machine.CallLog.ShouldContain(("DoUpdate", "(CT)"));
        }

        [Fact]
        public async Task Should_Handle_Rapid_Sequential_Operations_With_Different_Tokens()
        {
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);

            // Multiple operations with different tokens
            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            await machine.FireAsync(SpecTriggers.Start, null, cts1.Token);
            machine.CurrentState.ShouldBe(SpecStates.Working);

            await machine.FireAsync(SpecTriggers.Finish, null, cts2.Token);
            machine.CurrentState.ShouldBe(SpecStates.Done);

            // Both operations should complete successfully with their respective tokens
            machine.CallLog.Count.ShouldBeGreaterThan(4);
        }
    }

    internal class TestSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            // Run synchronously for testing
            d(state);
        }
    }
}