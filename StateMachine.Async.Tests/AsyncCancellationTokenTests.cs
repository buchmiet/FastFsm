using System;
using System.Threading;
using System.Threading.Tasks;
using StateMachine.Async.Tests.Machines;
using Xunit;

namespace StateMachine.Async.Tests;

public class AsyncCancellationTokenTests
{
    [Fact]
    public async Task Transition_CanBeCancelled_WithToken()
    {
        // Arrange
        var machine = new AsyncCancellableMachine(AsyncState.A);
        using var cts = new CancellationTokenSource();

        // Act
        // Rozpoczynamy operację, ale nie czekamy na jej zakończenie
        var fireTask = machine.FireAsync(AsyncTrigger.Go, null, cts.Token);

        // Natychmiast anulujemy operację
        cts.Cancel();

        // Assert
        // Teraz oczekiwanie na zadanie powinno rzucić wyjątek anulowania
        await Assert.ThrowsAsync<OperationCanceledException>(() => fireTask);

        // Stan maszyny pozostał nienaruszony
        Assert.Equal(AsyncState.A, machine.CurrentState);
    }
}