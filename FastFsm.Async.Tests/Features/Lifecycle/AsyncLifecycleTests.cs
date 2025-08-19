using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using StateMachine.Async.Tests.Features.Cancellation;

namespace StateMachine.Async.Tests.Features.Lifecycle
{
    public class AsyncLifecycleTests
    {
        [Fact]
        public async Task Machine_Throws_Before_StartAsync()
        {
            var machine = new BasicTokenMachine(TokenTestState.Initial);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await machine.TryFireAsync(TokenTestTrigger.Start));
        }

        [Fact]
        public async Task Machine_Works_After_StartAsync()
        {
            var machine = new BasicTokenMachine(TokenTestState.Initial);
            await machine.StartAsync();

            var result = await machine.TryFireAsync(TokenTestTrigger.Start);
            Assert.True(result);
            Assert.Equal(TokenTestState.Processing, machine.CurrentState);
        }
    }
}
