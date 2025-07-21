using System;
using System.Threading.Tasks;
using StateMachine.Async.Tests.Machines;
using Xunit;

namespace StateMachine.Async.Tests;

public class AsyncExceptionHandlingTests
{
    [Fact]
    public async Task Exception_In_AsyncCallback_IsPropagated_And_StateIsUnchanged()
    {
        // Arrange
        var machine = new AsyncExceptionMachine(AsyncState.A);

        // Act & Assert
        // Używamy Assert.ThrowsAsync do przechwycenia wyjątku z zadania (Task)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            machine.FireAsync(AsyncTrigger.Go)
        );

        // Najważniejsze: stan maszyny nie powinien się zmienić
        Assert.Equal(AsyncState.A, machine.CurrentState);
    }
}