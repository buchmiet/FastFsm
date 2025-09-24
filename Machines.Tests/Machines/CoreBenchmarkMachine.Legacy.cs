using FastFsm.Tests.Features.Performance;


namespace FastFsm.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class CoreBenchmarkMachineLegacy
{
    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.C)]
    [Transition(BenchmarkState.C, BenchmarkTrigger.Next, BenchmarkState.D)]
    [Transition(BenchmarkState.D, BenchmarkTrigger.Next, BenchmarkState.A)]
    private void Configure() { }
}