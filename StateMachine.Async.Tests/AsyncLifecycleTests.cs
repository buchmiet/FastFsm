using System.Threading.Tasks;
using StateMachine.Async.Tests.Machines;
using Xunit;

namespace StateMachine.Async.Tests;

public class AsyncLifecycleTests
{
    [Fact]
    public async Task AsyncCallbacks_AreExecuted_InCorrectOrder()
    {
        // Arrange
        var machine = new AsyncLifecycleMachine(AsyncState.A);

        // Act
        var result = await machine.TryFireAsync(AsyncTrigger.Go);

        // Assert
        Assert.True(result);
        Assert.Equal(AsyncState.B, machine.CurrentState);

        var expectedOrder = new[] { "ExitA", "ActionAtoB", "EntryB" };
        Assert.Equal(expectedOrder, machine.Log);
    }

    [Fact]
    public async Task CanFireAsync_And_GetPermittedTriggersAsync_WorkCorrectly()
    {
        // Arrange
        var machine = new AsyncLifecycleMachine(AsyncState.A);

        // Act & Assert
        Assert.True(await machine.CanFireAsync(AsyncTrigger.Go));
        Assert.False(await machine.CanFireAsync(AsyncTrigger.Next));

        var permitted = await machine.GetPermittedTriggersAsync();
        Assert.Single(permitted);
        Assert.Equal(AsyncTrigger.Go, permitted[0]);
    }
}