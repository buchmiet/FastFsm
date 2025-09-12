using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FastFsm.Tests.TestHelpers;

namespace FastFsm.Tests.Features.Core;

/// <summary>
/// Tests that verify no conversion happens when Fluent and Legacy use the same enum type
/// </summary>
public class EnumSameType_NoConversion_Tests
{
    public static IEnumerable<object[]> GetMachinesWithSameEnums()
    {
        // Get all machines that use the same enums for both APIs
        return MachineTypeRegistry.Types
            .Where(kvp => kvp.Value.UsesSameEnums)
            .Select(kvp => new object[] { kvp.Key, kvp.Value })
            .ToList();
    }
        
    [Theory]
    [MemberData(nameof(GetMachinesWithSameEnums))]
    public void SameType_StateConversion_ReturnsIdenticalValue(string machineName, EnumTypePair typePair)
    {
        // Arrange
        var stateType = typePair.FluentState; // Same as LegacyState
        var stateValues = Enum.GetValues(stateType);
            
        if (stateValues.Length == 0)
            return; // No values to test
                
        var firstState = stateValues.GetValue(0)!;
            
        // Act - Try to "convert" from Fluent to Legacy (should be no-op)
        var toLegacyMethod = typeof(EnumConverterV2)
            .GetMethod(nameof(EnumConverterV2.ToLegacy))!
            .MakeGenericMethod(stateType);
                
        var result = toLegacyMethod.Invoke(null, new[] { firstState, machineName });
            
        // Assert - Should be the exact same value
        Assert.Equal(firstState, result);
        Assert.Equal(firstState.GetType(), result!.GetType());
    }
        
    [Theory]
    [MemberData(nameof(GetMachinesWithSameEnums))]
    public void SameType_TriggerConversion_ReturnsIdenticalValue(string machineName, EnumTypePair typePair)
    {
        // Arrange
        var triggerType = typePair.FluentTrigger; // Same as LegacyTrigger
        var triggerValues = Enum.GetValues(triggerType);
            
        if (triggerValues.Length == 0)
            return; // No values to test
                
        var firstTrigger = triggerValues.GetValue(0)!;
            
        // Act - Try to "convert" from Legacy to Fluent (should be no-op)
        var toFluentMethod = typeof(EnumConverterV2)
            .GetMethod(nameof(EnumConverterV2.ToFluent))!
            .MakeGenericMethod(triggerType);
                
        var result = toFluentMethod.Invoke(null, new[] { firstTrigger, machineName });
            
        // Assert - Should be the exact same value
        Assert.Equal(firstTrigger, result);
        Assert.Equal(firstTrigger.GetType(), result!.GetType());
    }
        
    [Theory]
    [MemberData(nameof(GetMachinesWithSameEnums))]
    public void SameType_ValidateEnumParity_ReturnsTrue(string machineName, EnumTypePair typePair)
    {
        // Arrange & Act
        var validateMethod = typeof(EnumConverterV2)
            .GetMethods()
            .Where(m => m.Name == nameof(EnumConverterV2.ValidateEnumParity))
            .FirstOrDefault(m => m.GetParameters().Length == 1); // Get the tuple version
                
        Assert.NotNull(validateMethod);
            
        // Test state parity
        if (typePair.UsesSameStateEnum)
        {
            var stateMethod = validateMethod!.MakeGenericMethod(typePair.FluentState, typePair.LegacyState);
            var stateResult = stateMethod.Invoke(null, new object[] { machineName });
                
            Assert.NotNull(stateResult);
            var (isValid, errors) = ((bool, List<string>))stateResult;
                
            Assert.True(isValid, $"State parity should be true for {machineName} with same enum type");
            Assert.Empty(errors);
        }
            
        // Test trigger parity
        if (typePair.UsesSameTriggerEnum)
        {
            var triggerMethod = validateMethod!.MakeGenericMethod(typePair.FluentTrigger, typePair.LegacyTrigger);
            var triggerResult = triggerMethod.Invoke(null, new object[] { machineName });
                
            Assert.NotNull(triggerResult);
            var (isValid, errors) = ((bool, List<string>))triggerResult;
                
            Assert.True(isValid, $"Trigger parity should be true for {machineName} with same enum type");
            Assert.Empty(errors);
        }
    }
        
    [Fact]
    public void MachineTypeRegistry_HasExpectedMachines()
    {
        // Verify some key machines are registered with correct same-type mappings
            
        // InternalTransition should use same enums
        Assert.True(MachineTypeRegistry.Types.ContainsKey("InternalTransition"));
        var internalTransition = MachineTypeRegistry.Types["InternalTransition"];
        Assert.True(internalTransition.UsesSameEnums, "InternalTransition should use same enums for both APIs");
            
        // GuardPermitted should use same enums
        Assert.True(MachineTypeRegistry.Types.ContainsKey("GuardPermitted"));
        var guardPermitted = MachineTypeRegistry.Types["GuardPermitted"];
        Assert.True(guardPermitted.UsesSameEnums, "GuardPermitted should use same enums for both APIs");
            
        // CoreBenchmark should use DIFFERENT enums
        Assert.True(MachineTypeRegistry.Types.ContainsKey("CoreBenchmark"));
        var coreBenchmark = MachineTypeRegistry.Types["CoreBenchmark"];
        Assert.False(coreBenchmark.UsesSameEnums, "CoreBenchmark should use different enums for Fluent vs Legacy");
    }
        
    [Fact]
    public void StateMachineWrapperFactory_UsesCorrectTypes()
    {
        // Test that factory uses the correct types from registry
            
        // GuardPermitted should use shared enums from Features.Core
        var guardPermittedState = StateMachineWrapperFactory.GetStateEnum(
            "GuardPermitted", 
            StateMachineWrapperFactory.ApiType.Fluent, 
            "Idle");
                
        Assert.Equal(typeof(State), guardPermittedState.GetType());
        Assert.Equal(State.Idle, guardPermittedState);
            
        // InternalTransition should use StateCallbackTests types
        var internalState = StateMachineWrapperFactory.GetStateEnum(
            "InternalTransition",
            StateMachineWrapperFactory.ApiType.Legacy,
            "Active");
                
        Assert.Equal(typeof(StateCallbackTests.InternalState), internalState.GetType());
        Assert.Equal(StateCallbackTests.InternalState.Active, internalState);
    }
}