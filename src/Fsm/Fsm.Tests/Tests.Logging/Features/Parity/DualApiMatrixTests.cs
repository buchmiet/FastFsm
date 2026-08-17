using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Tests.Logging.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Logging.Features.Parity;

/// <summary>
/// Matrix tests verifying feature parity between Legacy (Attribute) and Fluent API
/// Tests all 16 machines with both API styles to ensure consistent behavior
/// </summary>
public class DualApiMatrixTests : LoggingTestBase
{
    public static IEnumerable<object[]> GetAllMachineApiCombinations()
    {
        var machines = MatrixConfig.GetAllMachineNames().ToList();
        var apis = new[] { 
            StateMachineWrapperFactoryComplete.ApiType.Legacy, 
            StateMachineWrapperFactoryComplete.ApiType.Fluent 
        };
        
        foreach (var machine in machines)
        {
            foreach (var api in apis)
            {
                yield return new object[] { machine, api };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetAllMachineApiCombinations))]
    public void Matrix_BasicSmokeTest_AllMachines(string machineName, StateMachineWrapperFactoryComplete.ApiType apiType)
    {
        // Arrange
        var config = MatrixConfig.GetConfig(machineName);
        config.ShouldNotBeNull($"Config for {machineName} should exist");
        
        var wrapper = StateMachineWrapperFactoryComplete.Create(
            machineName, 
            apiType, 
            config.InitialState,
            GetLogger<DualApiMatrixTests>()
        );

        // Act & Assert - Start machine
        Should.NotThrow(() => wrapper.Start());
        
        // Verify initial state
        wrapper.CurrentState.ShouldNotBeNull();
        
        // Try to get permitted triggers
        var permitted = wrapper.GetPermittedTriggers();
        permitted.ShouldNotBeNull();
        
        // If we have a trigger sequence, try the first trigger
        if (config.TriggerSequence.Length > 0)
        {
            var firstTrigger = config.TriggerSequence[0];
            var triggerEnum = StateMachineWrapperFactoryComplete.GetTriggerEnum(
                machineName, apiType, firstTrigger);
            
            // Check if we can fire this trigger
            var canFire = wrapper.CanFire(triggerEnum);
            
            // Try to fire if permitted
            if (canFire)
            {
                var payload = config.Payloads?.FirstOrDefault();
                var result = wrapper.TryFire(triggerEnum, payload);
                
                // For most machines, the first trigger should succeed
                // unless it has a guard that fails
                if (machineName != "GuardedStateMachine")
                {
                    result.ShouldBeTrue($"TryFire should succeed for {machineName} with {apiType}");
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetAllMachineApiCombinations))]
    public async Task Matrix_AsyncSmokeTest_AllMachines(string machineName, StateMachineWrapperFactoryComplete.ApiType apiType)
    {
        // Arrange
        var config = MatrixConfig.GetConfig(machineName);
        config.ShouldNotBeNull($"Config for {machineName} should exist");
        
        var wrapper = StateMachineWrapperFactoryComplete.Create(
            machineName, 
            apiType, 
            config.InitialState,
            GetLogger<DualApiMatrixTests>()
        );

        // Act & Assert - Start machine
        if (wrapper.Caps.HasFlag(ApiCapabilities.HasAsync) || machineName.Contains("Async"))
        {
            await Should.NotThrowAsync(async () => await wrapper.StartAsync());
        }
        else
        {
            wrapper.Start();
        }
        
        // For async-capable machines, test async firing
        if (config.TriggerSequence.Length > 0)
        {
            var firstTrigger = config.TriggerSequence[0];
            var triggerEnum = StateMachineWrapperFactoryComplete.GetTriggerEnum(
                machineName, apiType, firstTrigger);
            
            var payload = config.Payloads?.FirstOrDefault();
            
            if (wrapper.Caps.HasFlag(ApiCapabilities.HasAsync) || machineName.Contains("Async"))
            {
                var result = await wrapper.TryFireAsync(triggerEnum, payload);
                // AsyncLifecycleMachine should handle async triggers
                if (machineName == "AsyncLifecycleMachine")
                {
                    result.ShouldBeTrue($"Async TryFire should succeed for {machineName}");
                }
            }
        }
    }

    [Theory]
    [InlineData("PureStateMachine")]
    [InlineData("BasicStateMachine")]
    [InlineData("PayloadStateMachine")]
    [InlineData("ExtensionsStateMachine")]
    [InlineData("FullStateMachine")]
    [InlineData("MultiPayloadStateMachine")]
    public void Matrix_CoreMachines_BothApisProduceSameResults(string machineName)
    {
        // Arrange
        var config = MatrixConfig.GetConfig(machineName);
        config.ShouldNotBeNull();
        
        var legacyWrapper = StateMachineWrapperFactoryComplete.Create(
            machineName, 
            StateMachineWrapperFactoryComplete.ApiType.Legacy, 
            config.InitialState,
            GetLogger<DualApiMatrixTests>()
        );
        
        var fluentWrapper = StateMachineWrapperFactoryComplete.Create(
            machineName, 
            StateMachineWrapperFactoryComplete.ApiType.Fluent, 
            config.InitialState,
            GetLogger<DualApiMatrixTests>()
        );

        // Act - Start both machines
        legacyWrapper.Start();
        fluentWrapper.Start();
        
        // Assert - Both should be in same initial state
        legacyWrapper.CurrentState.ToString().ShouldBe(
            fluentWrapper.CurrentState.ToString(),
            $"Initial states should match for {machineName}"
        );
        
        // Execute trigger sequence and compare results
        foreach (var triggerName in config.TriggerSequence)
        {
            var legacyTrigger = StateMachineWrapperFactoryComplete.GetTriggerEnum(
                machineName, StateMachineWrapperFactoryComplete.ApiType.Legacy, triggerName);
            var fluentTrigger = StateMachineWrapperFactoryComplete.GetTriggerEnum(
                machineName, StateMachineWrapperFactoryComplete.ApiType.Fluent, triggerName);
            
            var payload = config.Payloads?.FirstOrDefault();
            
            var legacyResult = legacyWrapper.TryFire(legacyTrigger, payload);
            var fluentResult = fluentWrapper.TryFire(fluentTrigger, payload);
            
            // Results should match
            legacyResult.ShouldBe(fluentResult, 
                $"TryFire results should match for trigger {triggerName} in {machineName}");
            
            // States should still match
            legacyWrapper.CurrentState.ToString().ShouldBe(
                fluentWrapper.CurrentState.ToString(),
                $"States should match after trigger {triggerName} in {machineName}"
            );
        }
    }

    [Theory]
    [InlineData("HsmMachine")]
    public void Matrix_HierarchicalMachine_BothApisWork(string machineName)
    {
        // Arrange
        var config = MatrixConfig.GetConfig(machineName);
        config.ShouldNotBeNull();
        
        var legacyWrapper = StateMachineWrapperFactoryComplete.Create(
            machineName, 
            StateMachineWrapperFactoryComplete.ApiType.Legacy, 
            config.InitialState,
            GetLogger<DualApiMatrixTests>()
        );
        
        var fluentWrapper = StateMachineWrapperFactoryComplete.Create(
            machineName, 
            StateMachineWrapperFactoryComplete.ApiType.Fluent, 
            config.InitialState,
            GetLogger<DualApiMatrixTests>()
        );

        // Act - Start both machines
        legacyWrapper.Start();
        fluentWrapper.Start();
        
        // Assert - HSM capabilities
        legacyWrapper.Caps.ShouldHaveFlag(ApiCapabilities.IsHierarchical);
        fluentWrapper.Caps.ShouldHaveFlag(ApiCapabilities.IsHierarchical);
        
        // HSM machines may descend to child states on start
        // Both APIs should handle this consistently
        legacyWrapper.CurrentState.ShouldNotBeNull();
        fluentWrapper.CurrentState.ShouldNotBeNull();
    }

    [Fact]
    public void Matrix_AllMachinesRegistered_16Total()
    {
        // Verify we have all 16 machines registered
        var allMachines = MatrixConfig.GetAllMachineNames().ToList();
        allMachines.Count.ShouldBe(16, "Should have exactly 16 machines registered");
        
        // Verify each machine has both state and trigger types registered
        foreach (var machine in allMachines)
        {
            var config = MatrixConfig.GetConfig(machine);
            config.ShouldNotBeNull($"Config should exist for {machine}");
            
            // Verify type registry has entries
            Should.NotThrow(() => MachineTypeRegistry.GetStateType(machine, MachineTypeRegistry.Api.Legacy));
            Should.NotThrow(() => MachineTypeRegistry.GetStateType(machine, MachineTypeRegistry.Api.Fluent));
            Should.NotThrow(() => MachineTypeRegistry.GetTriggerType(machine, MachineTypeRegistry.Api.Legacy));
            Should.NotThrow(() => MachineTypeRegistry.GetTriggerType(machine, MachineTypeRegistry.Api.Fluent));
        }
    }

    [Fact]
    public void Matrix_PayloadMachines_HandlePayloadsCorrectly()
    {
        var payloadMachines = new[] 
        { 
            "PayloadStateMachine", 
            "FullStateMachine", 
            "MultiPayloadStateMachine",
            "FullMultiPayloadMachine"
        };
        
        foreach (var machineName in payloadMachines)
        {
            var config = MatrixConfig.GetConfig(machineName);
            config.ShouldNotBeNull();
            
            // Legacy API
            var legacyWrapper = StateMachineWrapperFactoryComplete.Create(
                machineName, 
                StateMachineWrapperFactoryComplete.ApiType.Legacy, 
                config.InitialState,
                GetLogger<DualApiMatrixTests>()
            );
            
            // Fluent API
            var fluentWrapper = StateMachineWrapperFactoryComplete.Create(
                machineName, 
                StateMachineWrapperFactoryComplete.ApiType.Fluent, 
                config.InitialState,
                GetLogger<DualApiMatrixTests>()
            );
            
            // Both should handle payloads
            if (machineName.Contains("Multi"))
            {
                legacyWrapper.Caps.ShouldHaveFlag(ApiCapabilities.HasMultiPayloads);
                fluentWrapper.Caps.ShouldHaveFlag(ApiCapabilities.HasMultiPayloads);
            }
            else
            {
                legacyWrapper.Caps.ShouldHaveFlag(ApiCapabilities.HasDefaultPayload);
                fluentWrapper.Caps.ShouldHaveFlag(ApiCapabilities.HasDefaultPayload);
            }
        }
    }

    [Fact]
    public void Matrix_SpecialCases_WorkCorrectly()
    {
        // Internal transitions
        var internalWrapper = StateMachineWrapperFactoryComplete.Create(
            "InternalTransitionMachine",
            StateMachineWrapperFactoryComplete.ApiType.Legacy,
            "Active",
            GetLogger<DualApiMatrixTests>()
        );
        internalWrapper.Caps.ShouldHaveFlag(ApiCapabilities.HasInternalTransitions);
        
        // Struct enums
        var structWrapper = StateMachineWrapperFactoryComplete.Create(
            "StructStateMachine",
            StateMachineWrapperFactoryComplete.ApiType.Legacy,
            "One",
            GetLogger<DualApiMatrixTests>()
        );
        structWrapper.CurrentState.ShouldNotBeNull();
    }
}