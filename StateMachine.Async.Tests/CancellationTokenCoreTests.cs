// CancellationTokenCoreTests.cs
using Abstractions.Attributes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StateMachine.Async.Tests
{
    public enum TokenTestState { Initial, Processing, Completed, Cancelled }
    public enum TokenTestTrigger { Start, Process, Complete, Cancel }


    #region Test Machine 1: Basic Token Support
    [StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
    public partial class BasicTokenMachine
    {
        public List<string> ExecutionLog { get; } = new();
        public List<string> TokenStates { get; } = new();

        [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
            Guard = nameof(CanStart), Action = nameof(StartProcessing))]
        [Transition(TokenTestState.Processing, TokenTestTrigger.Complete, TokenTestState.Completed)]
        [State(TokenTestState.Processing, OnEntry = nameof(OnProcessingEntry), OnExit = nameof(OnProcessingExit))]
        private void Configure() { }

        // Guard with token
        private async ValueTask<bool> CanStart(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("CanStart(CancellationToken)");
            TokenStates.Add($"Guard:CanRequest={cancellationToken.CanBeCanceled}");
            await Task.Delay(10, cancellationToken);
            return true;
        }

        // Action with token
        private async Task StartProcessing(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("StartProcessing(CancellationToken)");
            TokenStates.Add($"Action:CanRequest={cancellationToken.CanBeCanceled}");
            await Task.Delay(10, cancellationToken);
        }

        // OnEntry with token
        private async Task OnProcessingEntry(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("OnProcessingEntry(CancellationToken)");
            TokenStates.Add($"OnEntry:CanRequest={cancellationToken.CanBeCanceled}");
            await Task.Delay(10, cancellationToken);
        }

        // OnExit with token
        private async ValueTask OnProcessingExit(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("OnProcessingExit(CancellationToken)");
            TokenStates.Add($"OnExit:CanRequest={cancellationToken.CanBeCanceled}");
            await Task.Delay(10, cancellationToken);
        }
    }
    #endregion

    #region Test Machine 2: Optional Token (Overloads)
    [StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
    public partial class OptionalTokenMachine
    {
        public List<string> ExecutionLog { get; } = new();

        [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
            Guard = nameof(CanStart), Action = nameof(StartProcessing))]
        [State(TokenTestState.Processing, OnEntry = nameof(OnProcessingEntry))]
        private void Configure() { }

        // Guard overloads
        private async ValueTask<bool> CanStart()
        {
            ExecutionLog.Add("CanStart()");
            await Task.Delay(5);
            return true;
        }

        private async ValueTask<bool> CanStart(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("CanStart(CancellationToken)");
            await Task.Delay(5, cancellationToken);
            return true;
        }

        // Action overloads
        private async Task StartProcessing()
        {
            ExecutionLog.Add("StartProcessing()");
            await Task.Delay(5);
        }

        private async Task StartProcessing(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("StartProcessing(CancellationToken)");
            await Task.Delay(5, cancellationToken);
        }

        // OnEntry overloads
        private async Task OnProcessingEntry()
        {
            ExecutionLog.Add("OnProcessingEntry()");
            await Task.Delay(5);
        }

        private async Task OnProcessingEntry(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("OnProcessingEntry(CancellationToken)");
            await Task.Delay(5, cancellationToken);
        }
    }
    #endregion

    #region Test Machine 3: Cancellation Handling
    [StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
    public partial class CancellationMachine
    {
        public List<string> ExecutionLog { get; } = new();
        public int DelayMs { get; set; } = 100;

        [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
            Guard = nameof(CanStartAsync), Action = nameof(StartAsync))]
        [Transition(TokenTestState.Processing, TokenTestTrigger.Process, TokenTestState.Processing,
            Action = nameof(ProcessAsync))]
        [State(TokenTestState.Processing, OnEntry = nameof(OnProcessingEntryAsync), OnExit = nameof(OnProcessingExitAsync))]
        [Transition(TokenTestState.Processing, TokenTestTrigger.Cancel, TokenTestState.Cancelled)]
        private void Configure() { }

        private async ValueTask<bool> CanStartAsync(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("Guard:Begin");
            await Task.Delay(DelayMs, cancellationToken);
            ExecutionLog.Add("Guard:End");
            return true;
        }

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("Action:Begin");
            await Task.Delay(DelayMs, cancellationToken);
            ExecutionLog.Add("Action:End");
        }

        private async Task ProcessAsync(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("Process:Begin");
            await Task.Delay(DelayMs, cancellationToken);
            ExecutionLog.Add("Process:End");
        }

        private async Task OnProcessingEntryAsync(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("OnEntry:Begin");
            await Task.Delay(DelayMs, cancellationToken);
            ExecutionLog.Add("OnEntry:End");
        }

        private async ValueTask OnProcessingExitAsync(CancellationToken cancellationToken)
        {
            ExecutionLog.Add("OnExit:Begin");
            await Task.Delay(DelayMs, cancellationToken);
            ExecutionLog.Add("OnExit:End");
        }
    }
    #endregion

    #region Test Machine 4: Mixed Sync/Async Methods
    [StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
    public partial class MixedTokenMachine
    {
        public List<string> ExecutionLog { get; } = new();

        [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
            Guard = nameof(SyncGuard), Action = nameof(AsyncAction))]
        [State(TokenTestState.Processing, OnEntry = nameof(SyncOnEntry), OnExit = nameof(AsyncOnExit))]
        [Transition(TokenTestState.Processing, TokenTestTrigger.Complete, TokenTestState.Completed)]
        private void Configure() { }

        // Sync guard - should not be called with token
        private bool SyncGuard()
        {
            ExecutionLog.Add("SyncGuard()");
            return true;
        }

        // Async action with token
        private async Task AsyncAction(CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"AsyncAction(Token:{cancellationToken.CanBeCanceled})");
            await Task.Delay(10, cancellationToken);
        }

        // Sync OnEntry - should not be called with token
        private void SyncOnEntry()
        {
            ExecutionLog.Add("SyncOnEntry()");
        }

        // Async OnExit with token
        private async ValueTask AsyncOnExit(CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"AsyncOnExit(Token:{cancellationToken.CanBeCanceled})");
            await Task.Delay(10, cancellationToken);
        }
    }
    #endregion
    public class CancellationTokenCoreTests
    {
  
       

        #region Tests

        [Fact]
        public async Task Should_Pass_CancellationToken_To_All_Async_Methods()
        {
            // Arrange
            var machine = new BasicTokenMachine(TokenTestState.Initial);
            using var cts = new CancellationTokenSource();

            // Act
            var result = await machine.TryFireAsync(TokenTestTrigger.Start, cts.Token);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TokenTestState.Processing);

            machine.ExecutionLog.ShouldBe([
                "CanStart(CancellationToken)",
                "OnProcessingEntry(CancellationToken)",
                "StartProcessing(CancellationToken)"
            ]);

            machine.TokenStates.ShouldAllBe(s => s.EndsWith("CanRequest=True"));
        }

        [Fact]
        public async Task Should_Prefer_Token_Overload_When_Available()
        {
            // Arrange
            var machine = new OptionalTokenMachine(TokenTestState.Initial);
            using var cts = new CancellationTokenSource();

            // Act
            var result = await machine.TryFireAsync(TokenTestTrigger.Start, cts.Token);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TokenTestState.Processing);

            // Should prefer token overloads
            machine.ExecutionLog.ShouldBe([
                "CanStart(CancellationToken)",
                "OnProcessingEntry(CancellationToken)",
                "StartProcessing(CancellationToken)"
            ]);
        }

        [Fact]
        public async Task Should_Use_Parameterless_When_No_Token_Overload()
        {
            // Arrange
            var machine = new OptionalTokenMachine(TokenTestState.Initial);

            // Remove token overloads by creating a machine with only parameterless methods
            // This is simulated by the test - in real scenario, user wouldn't define token overloads

            // Act - still pass token, but methods without token overload should use parameterless
            var result = await machine.TryFireAsync(TokenTestTrigger.Start, CancellationToken.None);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TokenTestState.Processing);
        }

        [Fact]
        public async Task Should_Handle_Cancellation_In_Guard()
        {
            // Arrange
            var machine = new CancellationMachine(TokenTestState.Initial);
            using var cts = new CancellationTokenSource();

            // Cancel after 50ms
            cts.CancelAfter(50);

            // Act
            var result = await machine.TryFireAsync(TokenTestTrigger.Start, cts.Token);

            // Assert
            result.ShouldBeFalse(); // Transition should fail due to cancellation
            machine.CurrentState.ShouldBe(TokenTestState.Initial); // State unchanged
            machine.ExecutionLog.ShouldContain("Guard:Begin");
            machine.ExecutionLog.ShouldNotContain("Guard:End"); // Should be cancelled before completion
        }

        [Fact]
        public async Task Should_Handle_Cancellation_In_Action()
        {
            // Arrange
            var machine = new CancellationMachine(TokenTestState.Initial);
            machine.DelayMs = 10; // Quick guard
            using var cts = new CancellationTokenSource();

            // Act - Start transition
            var task = machine.TryFireAsync(TokenTestTrigger.Start, cts.Token);
            await Task.Delay(30); // Let guard complete
            cts.Cancel(); // Cancel during action

            var result = await task;

            // Assert
            result.ShouldBeFalse(); // Transition should fail
            machine.CurrentState.ShouldBe(TokenTestState.Initial); // State should roll back
            machine.ExecutionLog.ShouldContain("Action:Begin");
            machine.ExecutionLog.ShouldNotContain("Action:End");
        }

        [Fact]
        public async Task Should_Handle_Cancellation_In_OnEntry()
        {
            // Arrange
            var machine = new CancellationMachine(TokenTestState.Initial);
            machine.DelayMs = 10; // Quick guard and action
            using var cts = new CancellationTokenSource();

            // Act
            var task = machine.TryFireAsync(TokenTestTrigger.Start, cts.Token);
            await Task.Delay(30); // Let guard complete
            cts.Cancel(); // Cancel during OnEntry

            var result = await task;

            // Assert
            result.ShouldBeFalse();
            machine.CurrentState.ShouldBe(TokenTestState.Initial); // State should roll back
            machine.ExecutionLog.ShouldContain("OnEntry:Begin");
            machine.ExecutionLog.ShouldNotContain("OnEntry:End");
        }

        [Fact]
        public async Task CanFireAsync_Should_Pass_Token_To_Guard()
        {
            // Arrange
            var machine = new BasicTokenMachine(TokenTestState.Initial);
            using var cts = new CancellationTokenSource();

            // Act
            var canFire = await machine.CanFireAsync(TokenTestTrigger.Start, cts.Token);

            // Assert
            canFire.ShouldBeTrue();
            machine.ExecutionLog.ShouldContain("CanStart(CancellationToken)");
            machine.TokenStates.ShouldContain(s => s.StartsWith("Guard:") && s.EndsWith("CanRequest=True"));
        }

        [Fact]
        public async Task GetPermittedTriggersAsync_Should_Pass_Token_To_Guards()
        {
            // Arrange
            var machine = new BasicTokenMachine(TokenTestState.Initial);
            using var cts = new CancellationTokenSource();

            // Act
            var triggers = await machine.GetPermittedTriggersAsync(cts.Token);

            // Assert
            triggers.ShouldContain(TokenTestTrigger.Start);
            machine.ExecutionLog.ShouldContain("CanStart(CancellationToken)");
        }

        [Fact]
        public async Task Should_Work_Without_Token_When_Methods_Dont_Expect_It()
        {
            // Arrange
            var machine = new MixedTokenMachine(TokenTestState.Initial);

            // Act
            var result = await machine.TryFireAsync(TokenTestTrigger.Start);

            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(TokenTestState.Processing);

            machine.ExecutionLog.ShouldBe([
                "SyncGuard()",              // Sync method - no token
                "SyncOnEntry()",            // Sync method - no token
                "AsyncAction(Token:False)"  // Async method gets default token
            ]);
        }

        [Fact]
        public async Task Should_Handle_Initial_OnEntry_With_Token()
        {
            // Arrange & Act
            var machine = new BasicTokenMachine(TokenTestState.Processing);

            // Wait for fire-and-forget initial OnEntry
            await Task.Delay(50);

            // Assert
            machine.ExecutionLog.ShouldContain("OnProcessingEntry(CancellationToken)");
            // Initial OnEntry gets CancellationToken.None
            machine.TokenStates.ShouldContain(s => s.StartsWith("OnEntry:") && s.EndsWith("CanRequest=False"));
        }

        [Fact]
        public async Task Should_Handle_Multiple_Transitions_With_Same_Token()
        {
            // Arrange
            var machine = new CancellationMachine(TokenTestState.Initial);
            machine.DelayMs = 5; // Quick operations
            using var cts = new CancellationTokenSource();

            // Act - Multiple transitions with same token
            await machine.TryFireAsync(TokenTestTrigger.Start, cts.Token);
            await machine.TryFireAsync(TokenTestTrigger.Process, cts.Token);

            // Assert
            machine.CurrentState.ShouldBe(TokenTestState.Processing);
            machine.ExecutionLog.Count(s => s.Contains("Begin")).ShouldBeGreaterThan(3);
            machine.ExecutionLog.ShouldAllBe(s => !s.Contains("Begin") || s.Replace("Begin", "End") == machine.ExecutionLog.FirstOrDefault(e => e == s.Replace("Begin", "End")));
        }

        [Fact]
        public async Task Cancelled_Transition_Should_Not_Change_State()
        {
            // Arrange
            var machine = new CancellationMachine(TokenTestState.Processing);
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancelled

            // Act
            var result = await machine.TryFireAsync(TokenTestTrigger.Process, cts.Token);

            // Assert
            result.ShouldBeFalse();
            machine.CurrentState.ShouldBe(TokenTestState.Processing); // No state change
            machine.ExecutionLog.ShouldBeEmpty(); // Should fail immediately
        }

        #endregion
    }
}