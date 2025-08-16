using Abstractions.Attributes;
using static StateMachine.Tests.Features.Performance.BenchmarkTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
    public partial class WithGuardBenchmarkMachine
    {
        private int _counter;

        [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B, Guard = nameof(CanTransition))]
        [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.A, Guard = nameof(CanTransition))]
        private void Configure() { }

        private bool CanTransition()
        {
            _counter++;
            return _counter % 2 == 0; // Simple condition
        }
    }
}
