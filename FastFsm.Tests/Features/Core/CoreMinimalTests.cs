using System;
using Shouldly;
using FastFsm.Tests.Machines;
using FastFsm.Tests.Features.Performance;
using Xunit;

namespace FastFsm.Tests.Features.Core
{
    public class CoreMinimalTests
    {
        public enum ApiType { Fluent, Legacy }

        private object CreateMachine(ApiType apiType, object initialState)
        {
            if (apiType == ApiType.Fluent)
                return new CoreBenchmarkMachineFluent((BenchmarkTests.BenchmarkState)initialState);
            else
            {
                // Convert to Legacy enum
                var legacyState = (BenchmarkTestsLegacy.BenchmarkState)Enum.Parse(
                    typeof(BenchmarkTestsLegacy.BenchmarkState), 
                    initialState.ToString());
                return new CoreBenchmarkMachineLegacy(legacyState);
            }
        }

        private object GetBenchmarkState(ApiType apiType, string stateName)
        {
            return apiType == ApiType.Fluent
                ? Enum.Parse(typeof(BenchmarkTests.BenchmarkState), stateName)
                : Enum.Parse(typeof(BenchmarkTestsLegacy.BenchmarkState), stateName);
        }

        private object GetBenchmarkTrigger(ApiType apiType, string triggerName)
        {
            return apiType == ApiType.Fluent
                ? Enum.Parse(typeof(BenchmarkTests.BenchmarkTrigger), triggerName)
                : Enum.Parse(typeof(BenchmarkTestsLegacy.BenchmarkTrigger), triggerName);
        }

        private dynamic AsDynamic(object machine) => machine;
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_BasicTransitions_WorkCorrectly(ApiType apiType)
        {
            // Arrange
            var stateA = GetBenchmarkState(apiType, "A");
            var stateB = GetBenchmarkState(apiType, "B");
            var stateC = GetBenchmarkState(apiType, "C");
            var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            
            dynamic machine = CreateMachine(apiType, stateA);
            machine.Start();
            // Act & Assert
            Assert.Equal(stateA, machine.CurrentState);

            bool result = apiType == ApiType.Fluent
                ? ((CoreBenchmarkMachineFluent)machine).TryFire((BenchmarkTests.BenchmarkTrigger)triggerNext)
                : ((CoreBenchmarkMachineLegacy)machine).TryFire((BenchmarkTestsLegacy.BenchmarkTrigger)triggerNext);
            result.ShouldBeTrue();
            Assert.Equal(stateB, machine.CurrentState);

            result = machine.TryFire(triggerNext);
            result.ShouldBeTrue();
            Assert.Equal(stateC, machine.CurrentState);
        }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_InvalidTransition_ReturnsFalse(ApiType apiType)
        {
            // Arrange
            var stateA = GetBenchmarkState(apiType, "A");
            var triggerPrevious = GetBenchmarkTrigger(apiType, "Previous");
            
            dynamic machine = CreateMachine(apiType, stateA);
            machine.Start();

            // Act - Try invalid trigger
            var result = machine.TryFire(triggerPrevious);

            // Assert
            result.ShouldBeFalse();
            Assert.Equal(stateA, machine.CurrentState);
        }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_Fire_ThrowsOnInvalidTransition(ApiType apiType)
        {
            // Arrange
            var stateA = GetBenchmarkState(apiType, "A");
            var triggerPrevious = GetBenchmarkTrigger(apiType, "Previous");
            
            dynamic machine = CreateMachine(apiType, stateA);
            machine.Start();

            // Act & Assert
            Should.Throw<InvalidOperationException>(() =>
                machine.Fire(triggerPrevious));
        }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_GetPermittedTriggers_ReturnsCorrectTriggers(ApiType apiType)
        {
            // Arrange
            var stateB = GetBenchmarkState(apiType, "B");
            var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            
            dynamic machine = CreateMachine(apiType, stateB);
            machine.Start();

            // Act
            var permittedTriggers = machine.GetPermittedTriggers();

            // Assert
            permittedTriggers.ShouldContain(triggerNext);
            permittedTriggers.Count.ShouldBe(1);
        }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_CanFire_ChecksTransitions(ApiType apiType)
        {
            // Arrange
            var stateC = GetBenchmarkState(apiType, "C");
            var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            var triggerPrevious = GetBenchmarkTrigger(apiType, "Previous");
            
            dynamic machine = CreateMachine(apiType, stateC);
            machine.Start();

            // Act & Assert
            machine.CanFire(triggerNext).ShouldBeTrue();
            machine.CanFire(triggerPrevious).ShouldBeFalse();
        }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_MinimalMemoryFootprint(ApiType apiType)
        {
            // Arrange
            var stateA = GetBenchmarkState(apiType, "A");
            var initialMemory = GC.GetTotalMemory(true);
            var machines = new object[1000];

            // Act
            for (int i = 0; i < machines.Length; i++)
            {
                machines[i] = CreateMachine(apiType, stateA);
                ((dynamic)machines[i]).Start();
            }

            var finalMemory = GC.GetTotalMemory(true);
            var memoryPerInstance = (finalMemory - initialMemory) / machines.Length;

            // Assert - Pure variant should have minimal overhead
            memoryPerInstance.ShouldBeLessThan(200); // bytes per instance (adjusted for real-world overhead)
        }
    }


}
