using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Performance;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(BenchmarkTestsLegacy.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkTrigger))]
    public partial class WithGuardBenchmarkMachineLegacy
    {
        private int _counter;
        public bool ShouldAllow { get; set; } = true;

        [Transition(BenchmarkTestsLegacy.BenchmarkState.A, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.B, Guard = nameof(CanTransition))]
        [Transition(BenchmarkTestsLegacy.BenchmarkState.B, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.A, Guard = nameof(CanTransition))]
        private void Configure() { }

        private bool CanTransition()
        {
            return ShouldAllow; // Simple condition based on ShouldAllow flag
        }
    }
}
