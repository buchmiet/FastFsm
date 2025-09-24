using System;
using Shouldly;
using FastFsm.Tests.Features.Performance;
using FastFsm.Tests.Machines;
using Xunit;

namespace FastFsm.Tests.Features.Core;

public class CoreMinimalTests
{
    private static CoreBenchmarkMachineLegacy CreateMachine(BenchmarkState initialState)
    {
        return new CoreBenchmarkMachineLegacy(initialState);
    }

    [Fact]
    public void Core_BasicTransitions_WorkCorrectly()
    {
        var machine = CreateMachine(BenchmarkState.A);
        machine.Start();

        machine.CurrentState.ShouldBe(BenchmarkState.A);

        machine.TryFire(BenchmarkTrigger.Next).ShouldBeTrue();
        machine.CurrentState.ShouldBe(BenchmarkState.B);

        machine.TryFire(BenchmarkTrigger.Next).ShouldBeTrue();
        machine.CurrentState.ShouldBe(BenchmarkState.C);
    }

    [Fact]
    public void Core_InvalidTransition_ReturnsFalse()
    {
        var machine = CreateMachine(BenchmarkState.A);
        machine.Start();

        machine.TryFire(BenchmarkTrigger.Previous).ShouldBeFalse();
        machine.CurrentState.ShouldBe(BenchmarkState.A);
    }

    [Fact]
    public void Core_Fire_ThrowsOnInvalidTransition()
    {
        var machine = CreateMachine(BenchmarkState.A);
        machine.Start();

        Should.Throw<InvalidOperationException>(() => machine.Fire(BenchmarkTrigger.Previous));
    }

    [Fact]
    public void Core_GetPermittedTriggers_ReturnsCorrectTriggers()
    {
        var machine = CreateMachine(BenchmarkState.B);
        machine.Start();

        var permitted = machine.GetPermittedTriggers();

        permitted.ShouldContain(BenchmarkTrigger.Next);
        permitted.Count.ShouldBe(1);
    }

    [Fact]
    public void Core_CanFire_ChecksTransitions()
    {
        var machine = CreateMachine(BenchmarkState.C);
        machine.Start();

        machine.CanFire(BenchmarkTrigger.Next).ShouldBeTrue();
        machine.CanFire(BenchmarkTrigger.Previous).ShouldBeFalse();
    }

    [Fact]
    public void Core_MinimalMemoryFootprint()
    {
        const int instances = 200;
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        for (int i = 0; i < instances; i++)
        {
            var machine = CreateMachine(BenchmarkState.A);
            machine.Start();
        }

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var memoryPerInstance = (finalMemory - initialMemory) / instances;

        memoryPerInstance.ShouldBeLessThan(200);
    }
}
