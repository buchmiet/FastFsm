using System;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Lifecycle
{
    public class LifecycleTests
    {
        [Fact]
        public void Machine_Throws_Before_Start()
        {
            var machine = new CoreBenchmarkMachine(BenchmarkState.A);

            // TryFire without Start() should throw
            Assert.Throws<InvalidOperationException>(
                () => machine.TryFire(BenchmarkTrigger.Next));
        }

        [Fact]
        public void Machine_Works_After_Start()
        {
            var machine = new CoreBenchmarkMachine(BenchmarkState.A);
            machine.Start();

            Assert.True(machine.TryFire(BenchmarkTrigger.Next));
            Assert.Equal(BenchmarkState.B, machine.CurrentState);
        }
    }
}
