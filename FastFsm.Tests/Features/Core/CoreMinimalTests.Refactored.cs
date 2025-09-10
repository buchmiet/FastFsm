using System;
using Shouldly;
using FastFsm.Tests.TestHelpers;
using Xunit;
using static FastFsm.Tests.TestHelpers.StateMachineWrapperFactory;

namespace FastFsm.Tests.Features.Core
{
    /// <summary>
    /// Refactored version of CoreMinimalTests using the new wrapper infrastructure
    /// </summary>
    public class CoreMinimalTestsRefactored
    {
        private const string MachineType = "CoreBenchmark";
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_BasicTransitions_WorkCorrectly(ApiType apiType)
        {
            // Arrange
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "A");
            machine.Start();
            
            // Act & Assert
            var stateA = GetStateEnum(MachineType, apiType, "A");
            var stateB = GetStateEnum(MachineType, apiType, "B");
            var stateC = GetStateEnum(MachineType, apiType, "C");
            var triggerNext = GetTriggerEnum(MachineType, apiType, "Next");
            
            machine.CurrentState.ShouldBe(stateA);
            
            var result = machine.TryFire(triggerNext);
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(stateB);
            
            result = machine.TryFire(triggerNext);
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(stateC);
        }
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_InvalidTransition_ReturnsFalse(ApiType apiType)
        {
            // Arrange
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "A");
            machine.Start();
            
            var stateA = GetStateEnum(MachineType, apiType, "A");
            var triggerPrevious = GetTriggerEnum(MachineType, apiType, "Previous");
            
            // Act - Try invalid trigger
            var result = machine.TryFire(triggerPrevious);
            
            // Assert
            result.ShouldBeFalse();
            machine.CurrentState.ShouldBe(stateA);
        }
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void Core_Fire_ThrowsOnInvalidTransition(ApiType apiType)
        {
            // Arrange
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "A");
            machine.Start();
            
            var triggerPrevious = GetTriggerEnum(MachineType, apiType, "Previous");
            
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
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "B");
            machine.Start();
            
            var triggerNext = GetTriggerEnum(MachineType, apiType, "Next");
            
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
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "C");
            machine.Start();
            
            var triggerNext = GetTriggerEnum(MachineType, apiType, "Next");
            var triggerPrevious = GetTriggerEnum(MachineType, apiType, "Previous");
            
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
            var initialMemory = GC.GetTotalMemory(true);
            var machines = new IStateMachineTestWrapper[1000];
            
            // Act
            for (int i = 0; i < machines.Length; i++)
            {
                machines[i] = StateMachineWrapperFactory.Create(MachineType, apiType, "A");
                machines[i].Start();
            }
            
            var finalMemory = GC.GetTotalMemory(true);
            var memoryPerInstance = (finalMemory - initialMemory) / machines.Length;
            
            // Assert - Pure variant should have minimal overhead
            memoryPerInstance.ShouldBeLessThan(200); // bytes per instance
        }
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public async void Core_AsyncTransitions_WorkCorrectly(ApiType apiType)
        {
            // Arrange
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "A");
            await machine.StartAsync();
            
            var stateA = GetStateEnum(MachineType, apiType, "A");
            var stateB = GetStateEnum(MachineType, apiType, "B");
            var triggerNext = GetTriggerEnum(MachineType, apiType, "Next");
            
            machine.CurrentState.ShouldBe(stateA);
            
            // Act
            var result = await machine.TryFireAsync(triggerNext);
            
            // Assert
            result.ShouldBeTrue();
            machine.CurrentState.ShouldBe(stateB);
        }
        
        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public async void Core_AsyncFire_ThrowsOnInvalidTransition(ApiType apiType)
        {
            // Arrange
            var machine = StateMachineWrapperFactory.Create(MachineType, apiType, "A");
            await machine.StartAsync();
            
            var triggerPrevious = GetTriggerEnum(MachineType, apiType, "Previous");
            
            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(async () =>
                await machine.FireAsync(triggerPrevious));
        }
    }
}