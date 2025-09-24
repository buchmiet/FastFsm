using FastFsm.Tests.Features.Performance;


namespace FastFsm.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class WithGuardBenchmarkMachineLegacy
{
    private int _counter;
    public bool ShouldAllow { get; set; } = true;

    [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B, Guard = nameof(CanTransition))]
    [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A, Guard = nameof(CanTransition))]
    private void Configure() { }

    private bool CanTransition()
    {
        return ShouldAllow; // Simple condition based on ShouldAllow flag
    }
}