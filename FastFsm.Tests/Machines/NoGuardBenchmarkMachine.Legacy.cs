using FastFsm.Tests.Features.Performance;


namespace FastFsm.Tests.Machines;

[StateMachine(typeof(BenchmarkTestsLegacy.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachineLegacy
{
    [Transition(BenchmarkTestsLegacy.BenchmarkState.A, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.B)]
    [Transition(BenchmarkTestsLegacy.BenchmarkState.B, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.A)]
    private void ConfigureTransitions() { }
}