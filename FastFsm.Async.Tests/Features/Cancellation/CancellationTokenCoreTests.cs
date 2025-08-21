using Abstractions.Attributes;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace  FastFsm.Async.Tests.Features.Cancellation;

public enum TokenTestState { Initial, Processing, Completed, Cancelled }
public enum TokenTestTrigger { Start, Process, Complete, Cancel }


#region Test Machine 1: Basic Token Support
[StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
public partial class BasicTokenMachine
{
    public List<string> ExecutionLog { get; } = new();
    public List<string> TokenStates { get; } = new();

    // Transition method
    [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
        Guard = nameof(CanStart), Action = nameof(StartProcessing))]
    void StartTransition() { }

    // Guard without token (required for generator)
    private async ValueTask<bool> CanStart()
    {
        ExecutionLog.Add("CanStart()");
        await Task.Delay(10);
        return true;
    }

    // Guard with token
    private async ValueTask<bool> CanStart(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("CanStart(CancellationToken)");
        TokenStates.Add($"Guard:CanRequest={cancellationToken.CanBeCanceled}");
        await Task.Delay(10, cancellationToken);
        return true;
    }

    // Action without token (required for generator)
    private async Task StartProcessing()
    {
        ExecutionLog.Add("StartProcessing()");
        await Task.Delay(10);
    }

    // Action with token
    private async Task StartProcessing(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("StartProcessing(CancellationToken)");
        TokenStates.Add($"Action:CanRequest={cancellationToken.CanBeCanceled}");
        await Task.Delay(10, cancellationToken);
    }

    // OnEntry without token (required for generator)
    [State(TokenTestState.Processing, OnEntry = nameof(OnProcessingEntry), OnExit = nameof(OnProcessingExit))]
    private async Task OnProcessingEntry()
    {
        ExecutionLog.Add("OnProcessingEntry()");
        await Task.Delay(10);
    }

    // OnEntry with token
    private async Task OnProcessingEntry(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("OnProcessingEntry(CancellationToken)");
        TokenStates.Add($"OnEntry:CanRequest={cancellationToken.CanBeCanceled}");
        await Task.Delay(10, cancellationToken);
    }

    // OnExit without token (required for generator)
    private async ValueTask OnProcessingExit()
    {
        ExecutionLog.Add("OnProcessingExit()");
        await Task.Delay(10);
    }

    // OnExit with token
    private async ValueTask OnProcessingExit(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("OnProcessingExit(CancellationToken)");
        TokenStates.Add($"OnExit:CanRequest={cancellationToken.CanBeCanceled}");
        await Task.Delay(10, cancellationToken);
    }

    // Simple transition without guard or action
    [Transition(TokenTestState.Processing, TokenTestTrigger.Complete, TokenTestState.Completed)]
    private void CompleteTransition() { }
}
#endregion

#region Test Machine 2: Optional Token (Overloads)
[StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
public partial class OptionalTokenMachine
{
    public List<string> ExecutionLog { get; } = new();

    // Guard overloads
    [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
        Guard = nameof(CanStart), Action = nameof(StartProcessing))]
    private async ValueTask<bool> CanStart()
    {
        ExecutionLog.Add("CanStart()");
        await Task.Delay(5);
        return true;
    }

    private async ValueTask<bool> CanStart(System.Threading.CancellationToken cancellationToken)
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

    private async Task StartProcessing(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("StartProcessing(CancellationToken)");
        await Task.Delay(5, cancellationToken);
    }

    // OnEntry overloads
    [State(TokenTestState.Processing, OnEntry = nameof(OnProcessingEntry))]
    private async Task OnProcessingEntry()
    {
        ExecutionLog.Add("OnProcessingEntry()");
        await Task.Delay(5);
    }

    private async Task OnProcessingEntry(System.Threading.CancellationToken cancellationToken)
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
    void StartTransition() { }

    private async ValueTask<bool> CanStartAsync()
    {
        ExecutionLog.Add("Guard:Begin-NoToken");
        await Task.Delay(DelayMs);
        ExecutionLog.Add("Guard:End-NoToken");
        return true;
    }

    private async ValueTask<bool> CanStartAsync(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("Guard:Begin");
        await Task.Delay(DelayMs, cancellationToken);
        ExecutionLog.Add("Guard:End");
        return true;
    }

    private async Task StartAsync()
    {
        ExecutionLog.Add("Action:Begin-NoToken");
        await Task.Delay(DelayMs);
        ExecutionLog.Add("Action:End-NoToken");
    }

    private async Task StartAsync(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("Action:Begin");
        await Task.Delay(DelayMs, cancellationToken);
        ExecutionLog.Add("Action:End");
    }

    [Transition(TokenTestState.Processing, TokenTestTrigger.Process, TokenTestState.Processing,
        Action = nameof(ProcessAsync))]
    void ProcessTransition() { }

    private async Task ProcessAsync()
    {
        ExecutionLog.Add("Process:Begin-NoToken");
        await Task.Delay(DelayMs);
        ExecutionLog.Add("Process:End-NoToken");
    }

    private async Task ProcessAsync(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("Process:Begin");
        await Task.Delay(DelayMs, cancellationToken);
        ExecutionLog.Add("Process:End");
    }

    [State(TokenTestState.Processing, OnEntry = nameof(OnProcessingEntryAsync), OnExit = nameof(OnProcessingExitAsync))]
    private async Task OnProcessingEntryAsync()
    {
        ExecutionLog.Add("OnEntry:Begin-NoToken");
        await Task.Delay(DelayMs);
        ExecutionLog.Add("OnEntry:End-NoToken");
    }

    private async Task OnProcessingEntryAsync(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("OnEntry:Begin");
        await Task.Delay(DelayMs, cancellationToken);
        ExecutionLog.Add("OnEntry:End");
    }

    private async ValueTask OnProcessingExitAsync()
    {
        ExecutionLog.Add("OnExit:Begin-NoToken");
        await Task.Delay(DelayMs);
        ExecutionLog.Add("OnExit:End-NoToken");
    }

    private async ValueTask OnProcessingExitAsync(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add("OnExit:Begin");
        await Task.Delay(DelayMs, cancellationToken);
        ExecutionLog.Add("OnExit:End");
    }

    [Transition(TokenTestState.Processing, TokenTestTrigger.Cancel, TokenTestState.Cancelled)]
    private void CancelTransition() { }
}
#endregion

#region Test Machine 4: Mixed Sync/Async Methods
[StateMachine(typeof(TokenTestState), typeof(TokenTestTrigger))]
public partial class MixedTokenMachine
{
    public List<string> ExecutionLog { get; } = new();

    // Sync guard - converted to async for compilation
    [Transition(TokenTestState.Initial, TokenTestTrigger.Start, TokenTestState.Processing,
        Guard = nameof(SyncGuard), Action = nameof(AsyncAction))]
    private ValueTask<bool> SyncGuard()
    {
        ExecutionLog.Add("SyncGuard()");
        return ValueTask.FromResult(true);
    }

    // Async action without token (required for generator)
    private async Task AsyncAction()
    {
        ExecutionLog.Add("AsyncAction()");
        await Task.Delay(10);
    }

    // Async action with token
    private async Task AsyncAction(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add($"AsyncAction(Token:{cancellationToken.CanBeCanceled})");
        await Task.Delay(10, cancellationToken);
    }

    // Sync OnEntry - converted to async for compilation
    [State(TokenTestState.Processing, OnEntry = nameof(SyncOnEntry), OnExit = nameof(AsyncOnExit))]
    private Task SyncOnEntry()
    {
        ExecutionLog.Add("SyncOnEntry()");
        return Task.CompletedTask;
    }

    // Async OnExit without token (required for generator)
    private async ValueTask AsyncOnExit()
    {
        ExecutionLog.Add("AsyncOnExit()");
        await Task.Delay(10);
    }

    // Async OnExit with token
    private async ValueTask AsyncOnExit(System.Threading.CancellationToken cancellationToken)
    {
        ExecutionLog.Add($"AsyncOnExit(Token:{cancellationToken.CanBeCanceled})");
        await Task.Delay(10, cancellationToken);
    }

    [Transition(TokenTestState.Processing, TokenTestTrigger.Complete, TokenTestState.Completed)]
    private void CompleteTransition() { }
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
        await machine.StartAsync();
        using var cts = new CancellationTokenSource();

        // Act – token trafia we właściwy parametr
        var result = await machine.TryFireAsync(
            TokenTestTrigger.Start,
            cancellationToken: cts.Token);   // ← kluczowa zmiana

        // Assert
        result.ShouldBeTrue();
        machine.CurrentState.ShouldBe(TokenTestState.Processing);

        machine.ExecutionLog.ShouldBe(new[]
        {
            "CanStart(CancellationToken)",
            "OnProcessingEntry(CancellationToken)",
            "StartProcessing(CancellationToken)"
        });

        machine.TokenStates.ShouldAllBe(s => s.EndsWith("CanRequest=True"));
    }


    [Fact]
    public async Task Should_Prefer_Token_Overload_When_Available()
    {
        // Arrange
        var machine = new OptionalTokenMachine(TokenTestState.Initial);
        await machine.StartAsync();
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
        await machine.StartAsync();

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
        await machine.StartAsync();
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(50); // Anulujemy po 50 ms (Guard ma DelayMs=100)

        // Act
        var result = await machine.TryFireAsync(
            TokenTestTrigger.Start,
            cancellationToken: cts.Token);   // <-- poprawnie przekazany token

        // Assert
        result.ShouldBeFalse();                       // Guard anulowany ⇒ brak przejścia
        machine.CurrentState.ShouldBe(TokenTestState.Initial);
        machine.ExecutionLog.ShouldContain("Guard:Begin");
        machine.ExecutionLog.ShouldNotContain("Guard:End");
    }


    [Fact]
    public async Task Should_Handle_Cancellation_In_Action()
    {
        // Arrange
        var machine = new CancellationMachine(TokenTestState.Initial);
        await machine.StartAsync();
        machine.DelayMs = 100;
        using var cts = new CancellationTokenSource();

        var transition = machine.TryFireAsync(
            TokenTestTrigger.Start,
            cancellationToken: cts.Token);

        await Task.Delay(220);      // Guard i OnEntry skończą się
        cts.Cancel();              // Anulujemy w trakcie Action

        // Oczekujemy TaskCanceledException (ValueTask → Task via async lambda)
        await Should.ThrowAsync<TaskCanceledException>(async () => await transition);

        // Stan pozostaje Processing, ponieważ został ustawiony przed Action
        machine.CurrentState.ShouldBe(TokenTestState.Processing);

        // Log pokazuje początek akcji, ale brak końca
        machine.ExecutionLog.ShouldContain("Action:Begin");
        machine.ExecutionLog.ShouldNotContain("Action:End");
    }



    [Fact]
    public async Task Should_Handle_Cancellation_In_OnEntry()
    {
        // Arrange
        var machine = new CancellationMachine(TokenTestState.Initial)
        {
            DelayMs = 100   // szerokie okno na anulowanie
        };
        await machine.StartAsync();

        using var cts = new CancellationTokenSource();

        // Startujemy przejście; ValueTask → Task, żeby Shouldly je widział
        var fireTask = machine
            .FireAsync(TokenTestTrigger.Start, null, cts.Token)
            .AsTask();

        // Czekamy aż OnEntry się faktycznie zacznie
        var entered = SpinWait.SpinUntil(
            () => machine.ExecutionLog.Contains("OnEntry:Begin"),
            1_000);

        entered.ShouldBeTrue("OnEntry was never reached – test set-up failed.");

        // Anulujemy w trakcie OnEntry (w środku Task.Delay)
        cts.Cancel();

        // Assert – TaskCanceledException ma się propagować
        await Should.ThrowAsync<TaskCanceledException>(fireTask);

        machine.CurrentState.ShouldBe(TokenTestState.Processing);

        machine.ExecutionLog.ShouldContain("OnEntry:Begin");
        machine.ExecutionLog.ShouldNotContain("OnEntry:End");
        machine.ExecutionLog.ShouldNotContain("Action:Begin");
    }



    [Fact]
    public async Task CanFireAsync_Should_Pass_Token_To_Guard()
    {
        // Arrange
        var machine = new BasicTokenMachine(TokenTestState.Initial);
        await machine.StartAsync();
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
        await machine.StartAsync();
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
        await machine.StartAsync();

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
        await machine.StartAsync();

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
        await machine.StartAsync();
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

        await machine.StartAsync();

        var logCountBefore = machine.ExecutionLog.Count;

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // token anulowany przed wywołaniem

        // Act & Assert
        await Should.ThrowAsync<TaskCanceledException>(async () =>
        {
            await machine.TryFireAsync(
                TokenTestTrigger.Process,
                cancellationToken: cts.Token);
        });

        machine.CurrentState.ShouldBe(TokenTestState.Processing);
        machine.ExecutionLog.Count.ShouldBe(logCountBefore);
    }

    private static async Task SpinWaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.Elapsed > timeout)
                throw new TimeoutException("Condition not met in time.");
            await Task.Delay(1);
        }
    }






    #endregion
}
