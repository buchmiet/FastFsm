using Abstractions.Attributes;
using static StateMachine.Tests.Features.Performance.BenchmarkTests;

namespace StateMachine.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachine
{
    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A)]
    private void Configure() { }
}

