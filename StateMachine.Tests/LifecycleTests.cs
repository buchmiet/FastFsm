using StateMachine.Tests.Machines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static StateMachine.Tests.Performance.BenchmarkTests;

namespace StateMachine.Tests
{
    public class LifecycleTests
    {
        [Fact]
        public void Machine_Throws_Before_Start()
        {
            var machine = new PureBenchmarkMachine(BenchmarkState.A);

            // TryFire bez Start() powinien rzucać
            Assert.Throws<InvalidOperationException>(
                () => machine.TryFire(BenchmarkTrigger.Next));
        }

        [Fact]
        public void Machine_Works_After_Start()
        {
            var machine = new PureBenchmarkMachine(BenchmarkState.A);
            machine.Start();

            Assert.True(machine.TryFire(BenchmarkTrigger.Next));
            Assert.Equal(BenchmarkState.B, machine.CurrentState);
        }
    }
}
