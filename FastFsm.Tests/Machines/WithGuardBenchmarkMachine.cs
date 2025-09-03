using Abstractions.Attributes;
using Abstractions.Fluent;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
    public partial class WithGuardBenchmarkMachine
    {
        private int _counter;
        public bool ShouldAllow { get; set; } = true;

        [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B, Guard = nameof(CanTransition))]
        [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A, Guard = nameof(CanTransition))]
        private void Configure() { }

        private bool CanTransition()
        {
            _counter++;
            return ShouldAllow && (_counter % 2 == 0); // Simple condition
        }
    }
}
