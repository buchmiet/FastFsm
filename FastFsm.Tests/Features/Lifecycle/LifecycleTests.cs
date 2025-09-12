using FastFsm.Tests.Machines;
using System;
using Xunit;
using FastFsm.Tests.Features.Performance;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;

namespace FastFsm.Tests.Features.Lifecycle;

public class LifecycleTests
{
    public enum ApiType { Fluent, Legacy }

    private object CreateMachine(ApiType apiType, object initialState)
    {
        if (apiType == ApiType.Fluent)
            return new CoreBenchmarkMachineFluent((BenchmarkState)initialState);
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
            ? Enum.Parse(typeof(BenchmarkState), stateName)
            : Enum.Parse(typeof(BenchmarkTestsLegacy.BenchmarkState), stateName);
    }

    private object GetBenchmarkTrigger(ApiType apiType, string triggerName)
    {
        return apiType == ApiType.Fluent
            ? Enum.Parse(typeof(BenchmarkTrigger), triggerName)
            : Enum.Parse(typeof(BenchmarkTestsLegacy.BenchmarkTrigger), triggerName);
    }
    [Theory]
    [InlineData(ApiType.Fluent)]
    [InlineData(ApiType.Legacy)]
    public void Machine_Throws_Before_Start(ApiType apiType)
    {
        var stateA = GetBenchmarkState(apiType, "A");
        var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            
        dynamic machine = CreateMachine(apiType, stateA);

        // TryFire without Start() should throw
        if (apiType == ApiType.Fluent)
        {
            var fluentMachine = (CoreBenchmarkMachineFluent)machine;
            var fluentTrigger = (BenchmarkTests.BenchmarkTrigger)triggerNext;
            Assert.Throws<InvalidOperationException>(() => fluentMachine.TryFire(fluentTrigger));
        }
        else
        {
            var legacyMachine = (CoreBenchmarkMachineLegacy)machine;
            var legacyTrigger = (BenchmarkTestsLegacy.BenchmarkTrigger)triggerNext;
            Assert.Throws<InvalidOperationException>(() => legacyMachine.TryFire(legacyTrigger));
        }
    }

    [Theory]
    [InlineData(ApiType.Fluent)]
    [InlineData(ApiType.Legacy)]
    public void Machine_Works_After_Start(ApiType apiType)
    {
        var stateA = GetBenchmarkState(apiType, "A");
        var stateB = GetBenchmarkState(apiType, "B");
        var triggerNext = GetBenchmarkTrigger(apiType, "Next");
            
        dynamic machine = CreateMachine(apiType, stateA);
        machine.Start();

        bool result;
        if (apiType == ApiType.Fluent)
        {
            var fluentMachine = (CoreBenchmarkMachineFluent)machine;
            var fluentTrigger = (BenchmarkTests.BenchmarkTrigger)triggerNext;
            result = fluentMachine.TryFire(fluentTrigger);
        }
        else
        {
            var legacyMachine = (CoreBenchmarkMachineLegacy)machine;
            var legacyTrigger = (BenchmarkTestsLegacy.BenchmarkTrigger)triggerNext;
            result = legacyMachine.TryFire(legacyTrigger);
        }
        Assert.True(result);
        Assert.Equal(stateB, machine.CurrentState);
    }
}