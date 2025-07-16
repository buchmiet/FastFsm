using System;
using Shouldly;
using StateMachine.Tests.Machines;
using StateMachine.Tests.Performance;
using Xunit;

namespace StateMachine.Tests.PureVariant
{
    public class PureVariantTests
    {
        [Fact]
        public void PureVariant_BasicTransitions_WorkCorrectly()
        {
            // Arrange
            var machine = new PureBenchmarkMachine(BenchmarkTests.BenchmarkState.A);

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
        public void PureVariant_InvalidTransition_ReturnsFalse()
        {
            // Arrange
            var machine = new PureBenchmarkMachine(BenchmarkTests.BenchmarkState.A);

            // Act - Try invalid trigger
            var result = machine.TryFire(BenchmarkTests.BenchmarkTrigger.Previous);

            // Assert
            result.ShouldBeFalse();
            machine.CurrentState.ShouldBe(BenchmarkTests.BenchmarkState.A);
        }

        [Fact]
        public void PureVariant_Fire_ThrowsOnInvalidTransition()
        {
            // Arrange
            var machine = new PureBenchmarkMachine(BenchmarkTests.BenchmarkState.A);

            // Act & Assert
            Should.Throw<InvalidOperationException>(() =>
                machine.Fire(BenchmarkTests.BenchmarkTrigger.Previous));
        }

        [Fact]
        public void PureVariant_GetPermittedTriggers_ReturnsCorrectTriggers()
        {
            // Arrange
            var machine = new PureBenchmarkMachine(BenchmarkTests.BenchmarkState.B);

            // Act
            var permittedTriggers = machine.GetPermittedTriggers();

            // Assert
            permittedTriggers.ShouldContain(BenchmarkTests.BenchmarkTrigger.Next);
            permittedTriggers.Count.ShouldBe(1);
        }

        [Fact]
        public void PureVariant_CanFire_ChecksTransitions()
        {
            // Arrange
            var machine = new PureBenchmarkMachine(BenchmarkTests.BenchmarkState.C);

            // Act & Assert
            machine.CanFire(BenchmarkTests.BenchmarkTrigger.Next).ShouldBeTrue();
            machine.CanFire(BenchmarkTests.BenchmarkTrigger.Previous).ShouldBeFalse();
        }

        [Fact]
        public void PureVariant_MinimalMemoryFootprint()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var machines = new PureBenchmarkMachine[1000];

            // Act
            for (int i = 0; i < machines.Length; i++)
            {
                machines[i] = new PureBenchmarkMachine(BenchmarkTests.BenchmarkState.A);
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryPerInstance = (finalMemory - initialMemory) / machines.Length;

            // Assert - Pure variant should have minimal overhead
            memoryPerInstance.ShouldBeLessThan(200); // bytes per instance (adjusted for real-world overhead)
        }
    }


}