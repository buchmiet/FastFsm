using FastFsm.Tests.Features.Performance;


namespace FastFsm.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachineLegacy
{
    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A)]
    private void ConfigureTransitions() { }
}