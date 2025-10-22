using System;
using Shouldly;
using FastFsm.Tests.Machines.Legacy;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;
using Xunit;

namespace FastFsm.Tests.Core
{
    public class CoreMinimalTests
    {
        [Fact]
        public void Core_BasicTransitions_WorkCorrectly()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkState.A);
            machine.Start();
            // Act & Assert
            machine.CurrentState.ShouldBe(BenchmarkState.A);

            var result = machine.TryFire(BenchmarkTrigger.Next);
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(BenchmarkState.B);

            result = machine.TryFire(BenchmarkTrigger.Next);
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(BenchmarkState.C);
        }

        [Fact]
        public void Core_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkState.A);
            machine.Start();

            // Act - Try invalid trigger
            var result = machine.TryFire(BenchmarkTrigger.Previous);

            // Assert
            result.ShouldBeFalse();
            machine.CurrentState.ShouldBe(BenchmarkState.A);
        }

        [Fact]
        public void Core_Fire_ThrowsOnInvalidTransition()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkState.A);
            machine.Start();

            // Act & Assert
            Should.Throw<InvalidOperationException>(() =>
                machine.Fire(BenchmarkTrigger.Previous));
        }

        [Fact]
        public void Core_GetPermittedTriggers_ReturnsCorrectTriggers()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkState.B);
            machine.Start();

            // Act
            var permittedTriggers = machine.GetPermittedTriggers();

            // Assert
            permittedTriggers.ShouldContain(BenchmarkTrigger.Next);
            permittedTriggers.Count.ShouldBe(1);
        }

        [Fact]
        public void Core_CanFire_ChecksTransitions()
        {
            // Arrange
            var machine = new CoreBenchmarkMachine(BenchmarkState.C);
            machine.Start();

            // Act & Assert
            machine.CanFire(BenchmarkTrigger.Next).ShouldBeTrue();
            machine.CanFire(BenchmarkTrigger.Previous).ShouldBeFalse();
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
                machines[i] = new CoreBenchmarkMachine(BenchmarkState.A);
                machines[i].Start();
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryPerInstance = (finalMemory - initialMemory) / machines.Length;

            // Assert - Pure variant should have minimal overhead
            memoryPerInstance.ShouldBeLessThan(200); // bytes per instance (adjusted for real-world overhead)
        }
    }


}
