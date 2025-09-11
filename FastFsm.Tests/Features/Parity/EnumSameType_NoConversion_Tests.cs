using System;
using Shouldly;
using Xunit;
using FastFsm.Tests.TestHelpers;

namespace FastFsm.Tests.Features.Parity;

public class EnumSameType_NoConversion_Tests
{
    [Theory]
    [InlineData("GuardPermitted")]
    [InlineData("InternalTransition")]
    [InlineData("ExceptionCallback")]
    [InlineData("PayloadStateMachine")]
    [InlineData("FullMultiPayload")]
    public void ToFluent_ToLegacy_ShouldBePassThrough_ForSameEnums(string machine)
    {
        // Arrange: get enum types from registry
        var fluentStateType = MachineTypeRegistry.GetStateType(machine, Api.Fluent);
        var legacyStateType = MachineTypeRegistry.GetStateType(machine, Api.Legacy);
        var fluentTriggerType = MachineTypeRegistry.GetTriggerType(machine, Api.Fluent);
        var legacyTriggerType = MachineTypeRegistry.GetTriggerType(machine, Api.Legacy);

        // Skip if any differs (the test targets same-enum cases)
        if (fluentStateType != legacyStateType || fluentTriggerType != legacyTriggerType)
            return;

        // Pick first state/trigger names
        var stateNames = Enum.GetNames(fluentStateType);
        var triggerNames = Enum.GetNames(fluentTriggerType);
        stateNames.Length.ShouldBeGreaterThan(0);
        triggerNames.Length.ShouldBeGreaterThan(0);

        var state = Enum.Parse(fluentStateType, stateNames[0]);
        var trigger = Enum.Parse(fluentTriggerType, triggerNames[0]);

        // Act: run through converter both directions
        var toLegacyState = typeof(EnumConverterV2)
            .GetMethod(nameof(EnumConverterV2.ToLegacy))!
            .MakeGenericMethod(legacyStateType)
            .Invoke(null, new[] { state!, machine });
        var toFluentState = typeof(EnumConverterV2)
            .GetMethod(nameof(EnumConverterV2.ToFluent))!
            .MakeGenericMethod(fluentStateType)
            .Invoke(null, new[] { toLegacyState!, machine });

        var toLegacyTrigger = typeof(EnumConverterV2)
            .GetMethod(nameof(EnumConverterV2.ToLegacy))!
            .MakeGenericMethod(legacyTriggerType)
            .Invoke(null, new[] { trigger!, machine });
        var toFluentTrigger = typeof(EnumConverterV2)
            .GetMethod(nameof(EnumConverterV2.ToFluent))!
            .MakeGenericMethod(fluentTriggerType)
            .Invoke(null, new[] { toLegacyTrigger!, machine });

        // Assert: values round-trip and equality holds
        toLegacyState.ShouldBe(state); // same type, pass-through equality
        toFluentState.ShouldBe(state);

        toLegacyTrigger.ShouldBe(trigger);
        toFluentTrigger.ShouldBe(trigger);
    }
}

