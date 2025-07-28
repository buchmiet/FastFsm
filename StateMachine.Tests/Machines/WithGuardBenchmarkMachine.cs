using Abstractions.Attributes;
using static StateMachine.Tests.Performance.BenchmarkTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
    [GenerationMode(GenerationMode.Pure, Force = true)]
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
