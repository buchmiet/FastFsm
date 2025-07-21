using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator.Async.Tests.Implementations;
using Xunit;

namespace Generator.Async.Tests;

public class AsyncStateMachineBaseTests
{
    // Test 1: Weryfikacja, że synchroniczne API jest zablokowane
    [Fact]
    public void SyncApi_WhenCalledOnAsyncMachine_ThrowsInvalidOperationException()
    {
        // Arrange
        var fsm = new TestAsyncFsm(TestState.A);

        // Act & Assert
        var exTryFire = Assert.Throws<InvalidOperationException>(() => fsm.TryFire(TestTrigger.Go));
        var exFire = Assert.Throws<InvalidOperationException>(() => fsm.Fire(TestTrigger.Go));
        var exCanFire = Assert.Throws<InvalidOperationException>(() => fsm.CanFire(TestTrigger.Go));
        var exGetTriggers = Assert.Throws<InvalidOperationException>(() => fsm.GetPermittedTriggers());

        // Sprawdź, czy komunikaty o błędach są pomocne
        Assert.Contains("Use the 'TryFireAsync' method instead", exTryFire.Message);
        Assert.Contains("Use the 'FireAsync' method instead", exFire.Message);
        Assert.Contains("Use the 'CanFireAsync' method instead", exCanFire.Message);
        Assert.Contains("Use the 'GetPermittedTriggersAsync' method instead", exGetTriggers.Message);
    }

    // Test 2: Weryfikacja, że CancellationToken jest respektowany PRZED wejściem do semafora
    [Fact]
    public async Task TryFireAsync_WithPreCancelledToken_ThrowsCorrectCancellationException()
    {
        // Arrange
        var fsm = new TestAsyncFsm(TestState.A);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Anuluj token natychmiast

        // Act & Assert
        // Zmieniamy oczekiwany typ na TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            fsm.TryFireAsync(TestTrigger.Go, null, cts.Token).AsTask()
        );

        // Upewnij się, że logika wewnętrzna nie została wykonana
        Assert.False(fsm.WasInternalLogicCalled);
    }

    // Test 3: Weryfikacja, że SemaphoreSlim chroni przed jednoczesnym dostępem
    [Fact]
    public async Task TryFireAsync_WhenCalledConcurrently_IsThreadSafe()
    {
        // Arrange
        const int delayMs = 100;
        const int concurrentCalls = 5;
        var fsm = new TestAsyncFsm(TestState.A, internalDelayMs: delayMs);

        // Zmienna do śledzenia, czy którekolwiek zadanie zakończyło się przedwcześnie
        long completedTasksCount = 0;

        // Act
        // Uruchom kilka zadań jednocześnie, które próbują wejść do metody
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(async _ =>
            {
                await fsm.TryFireAsync(TestTrigger.Go);
                Interlocked.Increment(ref completedTasksCount);
            })
            .ToList();

        // Daj zadaniom chwilę na wystartowanie i potencjalne zablokowanie na semaforze
        await Task.Delay(delayMs / 2);

        // W tym momencie tylko jedno zadanie powinno być wewnątrz sekcji krytycznej,
        // a reszta powinna czekać. Dlatego żadne nie powinno być jeszcze ukończone.
        Assert.Equal(0, Interlocked.Read(ref completedTasksCount));

        // Poczekaj na zakończenie wszystkich zadań
        await Task.WhenAll(tasks);

        // Assert
        // Wszystkie zadania powinny się zakończyć pomyślnie
        Assert.Equal(concurrentCalls, completedTasksCount);

        // Stan końcowy powinien być wynikiem ostatniego wykonania
        Assert.Equal(TestState.B, fsm.CurrentState);
    }

    [Fact]
    public async Task FireAsync_WhenTransitionSucceeds_CompletesSuccessfully()
    {
        // Arrange
        var fsm = new TestAsyncFsm(TestState.A);

        // Act
        await fsm.FireAsync(TestTrigger.Go);

        // Assert
        Assert.Equal(TestState.B, fsm.CurrentState);
    }
}