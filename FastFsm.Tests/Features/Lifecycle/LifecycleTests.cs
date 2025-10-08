using System;
using FastFsm.Tests.Features.Performance;
using Machines.Tests.Features.Performance;
using Machines.Tests.Machines;
using Xunit;

namespace FastFsm.Tests.Features.Lifecycle;

public class LifecycleTests
{
    [Fact]
    public void Machine_Throws_Before_Start()
    {
        var machine = new CoreBenchmarkMachineLegacy(BenchmarkState.A);
        Assert.Throws<InvalidOperationException>(() => machine.TryFire(BenchmarkTrigger.Next));
    }

    [Fact]
    public void Machine_Works_After_Start()
    {
        var machine = new CoreBenchmarkMachineLegacy(BenchmarkState.A);
        machine.Start();

        var result = machine.TryFire(BenchmarkTrigger.Next);
        Assert.True(result);
        Assert.Equal(BenchmarkState.B, machine.CurrentState);
    }
}
