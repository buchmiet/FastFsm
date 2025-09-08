using Abstractions.Attributes;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachine
{
    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A)]
    private void Configure() { }
}

