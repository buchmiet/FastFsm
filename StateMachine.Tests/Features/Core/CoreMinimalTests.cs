using System;
using Shouldly;
using StateMachine.Tests.Machines;
using StateMachine.Tests.Features.Performance;
using Xunit;

namespace StateMachine.Tests.Features.Core
{
    public class CoreMinimalTests
    {
        [Fact]
        public void Core_BasicTransitions_WorkCorrectly()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkTests.BenchmarkState.A);
            machine.Start();
            // Act & Assert
            machine.CurrentState.ShouldBe(BenchmarkTests.BenchmarkState.A);

            var result = machine.TryFire(BenchmarkTests.BenchmarkTrigger.Next);
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(BenchmarkTests.BenchmarkState.B);

            result = machine.TryFire(BenchmarkTests.BenchmarkTrigger.Next);
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(BenchmarkTests.BenchmarkState.C);
        }

        [Fact]
        public void Core_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkTests.BenchmarkState.A);
            machine.Start();

            // Act - Try invalid trigger
            var result = machine.TryFire(BenchmarkTests.BenchmarkTrigger.Previous);

            // Assert
            result.ShouldBeFalse();
            machine.CurrentState.ShouldBe(BenchmarkTests.BenchmarkState.A);
        }

        [Fact]
        public void Core_Fire_ThrowsOnInvalidTransition()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkTests.BenchmarkState.A);
            machine.Start();

            // Act & Assert
            Should.Throw<InvalidOperationException>(() =>
                machine.Fire(BenchmarkTests.BenchmarkTrigger.Previous));
        }

        [Fact]
        public void Core_GetPermittedTriggers_ReturnsCorrectTriggers()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkTests.BenchmarkState.B);
            machine.Start();

            // Act
            var permittedTriggers = machine.GetPermittedTriggers();

            // Assert
            permittedTriggers.ShouldContain(BenchmarkTests.BenchmarkTrigger.Next);
            permittedTriggers.Count.ShouldBe(1);
        }

        [Fact]
        public void Core_CanFire_ChecksTransitions()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkTests.BenchmarkState.C);
            machine.Start();

            // Act & Assert
            machine.CanFire(BenchmarkTests.BenchmarkTrigger.Next).ShouldBeTrue();
            machine.CanFire(BenchmarkTests.BenchmarkTrigger.Previous).ShouldBeFalse();
        }

        [Fact]
        public void Core_MinimalMemoryFootprint()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var machines = new CoreBenchmarkMachine[1000];

            // Act
            for (int i = 0; i < machines.Length; i++)
            {
                machines[i] = new CoreBenchmarkMachine(BenchmarkTests.BenchmarkState.A);
                machines[i].Start();
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryPerInstance = (finalMemory - initialMemory) / machines.Length;

            // Assert - Pure variant should have minimal overhead
            memoryPerInstance.ShouldBeLessThan(200); // bytes per instance (adjusted for real-world overhead)
        }
    }


}
