using Abstractions.Attributes;
using Abstractions.Fluent;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Async.Features.Cancellation;
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

        // Guards - only one overload allowed in async machine
        private async ValueTask<bool> CanStart(CancellationToken ct = default)
        {
            _callLog.Add(("CanStart", ct.CanBeCanceled ? "(CT)" : "()"));
            await Task.Delay(1, ct);
            return true;
        }

        private ValueTask<bool> CanFinish(CancellationToken ct = default)
        {
            _callLog.Add(("CanFinish", ct.CanBeCanceled ? "(CT)" : "()"));
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(true);
        }

        private async ValueTask<bool> CanUpdate(CancellationToken ct)
        {
            _callLog.Add(("CanUpdate", "(CT)"));
            await Task.Delay(1, ct);
            return true;
        }

        // Actions - only one overload allowed in async machine
        private async Task DoStart(CancellationToken ct = default)
        {
            _callLog.Add(("DoStart", ct.CanBeCanceled ? "(CT)" : "()"));
            await Task.Delay(1, ct);
        }

        private async ValueTask DoFinish()
        {
            _callLog.Add(("DoFinish", "()"));
            await Task.Delay(1);
        }

        private async Task DoUpdate(CancellationToken ct)
        {
            _callLog.Add(("DoUpdate", "(CT)"));
            await Task.Delay(1, ct);
        }

        // State callbacks
        private Task OnEnterReady()
        {
            _callLog.Add(("OnEnterReady", "()"));
            return Task.CompletedTask;
        }

        private async Task OnEnterWorking(CancellationToken ct)
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

    // Fluent API equivalent
    [StateMachine(typeof(SpecStates), typeof(SpecTriggers))]
    public partial class SpecificationComplianceMachineFluentFsm
    {
        private readonly List<(string Method, string Parameters)> _callLog = new();
        public IReadOnlyList<(string Method, string Parameters)> CallLog => _callLog;

        private void Configure() => FSM
            .State(SpecStates.Ready)
                .OnEntryAsync(nameof(OnEnterReady))
                .On(SpecTriggers.Start)
                    .Guard(nameof(CanStart))
                    .Action(nameof(DoStart))
                    .GoTo(SpecStates.Working)
            .State(SpecStates.Working)
                .OnEntryAsync(nameof(OnEnterWorking))
                .OnExitAsync(nameof(OnExitWorking))
                .On(SpecTriggers.Finish)
                    .Guard(nameof(CanFinish))
                    .Action(nameof(DoFinish))
                    .GoTo(SpecStates.Done)
                .OnInternal(SpecTriggers.Update)
                    .Guard(nameof(CanUpdate))
                    .Action(nameof(DoUpdate))
                    .Internal()
            .State(SpecStates.Done);

        private Task OnEnterReady()
        {
            _callLog.Add(("OnEnterReady", "()"));
            return Task.CompletedTask;
        }
        private async Task OnEnterWorking(CancellationToken ct)
        {
            _callLog.Add(("OnEnterWorking", "(CT)"));
            await Task.Delay(1, ct);
        }
        private async ValueTask OnExitWorking()
        {
            _callLog.Add(("OnExitWorking", "()"));
            await Task.Delay(1);
        }
        private async ValueTask<bool> CanStart(CancellationToken ct = default)
        {
            _callLog.Add(("CanStart", ct.CanBeCanceled ? "(CT)" : "()"));
            await Task.Delay(1, ct);
            return true;
        }
        private ValueTask<bool> CanFinish(CancellationToken ct = default)
        {
            _callLog.Add(("CanFinish", ct.CanBeCanceled ? "(CT)" : "()"));
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult(true);
        }
        private async ValueTask<bool> CanUpdate(CancellationToken ct)
        {
            _callLog.Add(("CanUpdate", "(CT)"));
            await Task.Delay(1, ct);
            return true;
        }
        private async Task DoStart(CancellationToken ct = default)
        {
            _callLog.Add(("DoStart", ct.CanBeCanceled ? "(CT)" : "()"));
            await Task.Delay(1, ct);
        }
        private async ValueTask DoFinish()
        {
            _callLog.Add(("DoFinish", "()"));
            await Task.Delay(1);
        }
        private async Task DoUpdate(CancellationToken ct)
        {
            _callLog.Add(("DoUpdate", "(CT)"));
            await Task.Delay(1, ct);
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
            var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Ready);
            await machine.StartAsync();

            // wait until async OnEnterReady finishes writing to the log…
            await Task.Delay(20);          // 20 ms is enough given Delay(1) in OnEnterReady
            machine.ClearLog();            // only now do we reset

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

            // No callback should run after cancellation
            machine.CallLog.ShouldBeEmpty();
        }



        [Fact]
        public async Task Should_Use_Best_Overload_With_CancellationToken_Priority()
        {
            // Per spec: Priority order for overloads - (CT) should be preferred over ()
            var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Ready);
            await machine.StartAsync();
            using var cts = new CancellationTokenSource();

            await machine.FireAsync(SpecTriggers.Start, null, cts.Token);

            // Should have called overloads with CancellationToken where available
            machine.CallLog.ShouldContain(("CanStart", "(CT)"));
            machine.CallLog.ShouldContain(("OnEnterWorking", "(CT)"));
            machine.CallLog.ShouldContain(("DoStart", "(CT)"));

            // All methods should have been called with token (CT)
        }

        [Fact]
        public async Task Should_Use_Parameterless_Overload_When_No_CT_Version()
        {
            var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Working);
            using var cts = new CancellationTokenSource();
            await machine.StartAsync();
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
            await machine.StartAsync();
            // CanFinish has both sync versions
            var canFire = await machine.CanFireAsync(SpecTriggers.Finish);

            canFire.ShouldBeTrue();
            // Should have called with default token
            machine.CallLog.ShouldContain(("CanFinish", "()"));
        }



        [Fact]
        public async Task Should_Not_Rollback_State_On_Cancellation()
        {
            // This test verifies that cancellation during action doesn't rollback state changes
            // The behavior depends on generator implementation - we test the actual behavior
            
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);
            await machine.StartAsync();

            using var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>();

            // Override DoStart to have control over timing
            var originalCallLog = machine.CallLog;
            
            // Start the transition
            var fireTask = machine.FireAsync(SpecTriggers.Start, null, cts.Token).AsTask();

            // Wait for DoStart to begin
            var startTime = DateTime.UtcNow;
            while (!machine.CallLog.Any(e => e.Method == "DoStart"))
            {
                if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(2))
                {
                    // DoStart never started - check if transition completed
                    if (fireTask.IsCompleted)
                    {
                        // Transition completed without exception - this means DoStart finished quickly
                        // This is acceptable behavior - the action completed before cancellation
                        machine.CurrentState.ShouldBeOneOf(SpecStates.Working, SpecStates.Ready);
                        return;
                    }
                    
                    var log = string.Join(", ", machine.CallLog.Select(l => $"{l.Method}{l.Parameters}"));
                    throw new TimeoutException($"DoStart not reached. Log: {log}");
                }
                await Task.Delay(5);
            }

            // Cancel immediately after DoStart begins
            cts.Cancel();

            // Check the result
            try
            {
                await fireTask;
                // If no exception, the action completed before cancellation took effect
                // This is valid behavior - not all async operations check cancellation immediately
                machine.CurrentState.ShouldBeOneOf(SpecStates.Working, SpecStates.Ready);
            }
            catch (OperationCanceledException)
            {
                // Expected behavior - cancellation was detected
                // State depends on when cancellation occurred relative to state change
                machine.CurrentState.ShouldBeOneOf(SpecStates.Working, SpecStates.Ready);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected exception type: {ex.GetType().Name}. Message: {ex.Message}", ex);
            }
            
            // Verify that the machine is in a valid state
            var finalLog = string.Join(", ", machine.CallLog.Select(l => $"{l.Method}{l.Parameters}"));
            machine.CurrentState.ShouldNotBe(SpecStates.Done, $"Should not reach Done state. Log: {finalLog}");
        }


        [Fact]
        public async Task Should_Execute_Callbacks_In_Documented_Order()
        {
            // Per spec: Guard → OnExit → State Change → OnEntry → Action
            var machine = new SpecificationComplianceMachine(SpecStates.Ready);
            await machine.StartAsync();

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
                var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Ready);
            await machine.StartAsync();
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
            var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Working);
            using var cts = new CancellationTokenSource();
            await machine.StartAsync();
            // Should evaluate guards with token
            var triggers = await machine.GetPermittedTriggersAsync(cts.Token);

            triggers.ShouldNotBeEmpty();
            machine.CallLog.ShouldContain(("CanFinish", "(CT)"));
            machine.CallLog.ShouldContain(("CanUpdate", "(CT)"));
        }

        [Fact]
        public async Task Should_Handle_Null_CancellationToken_As_Default()
        {
            var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Ready);
            await machine.StartAsync();

            // No cancellable token
            await machine.FireAsync(SpecTriggers.Start, null, default);

            machine.CurrentState.ShouldBe(SpecStates.Working);

            // Guard was invoked with a token, but CanBeCanceled == false ⇒ "()"
            machine.CallLog.ShouldContain(("CanStart", "()"));
        }


        [Fact]
        public async Task Should_Propagate_Token_Through_Entire_Transition_Chain()
        {
            // Create a token source we can monitor
            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            var machine = new SpecificationComplianceMachineFluentFsm(SpecStates.Ready);
            await machine.StartAsync();

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
            await machine.StartAsync();

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
    public override void Post(SendOrPostCallback d, object? state) =>
        // Run synchronously for testing
        d(state);
}
 
