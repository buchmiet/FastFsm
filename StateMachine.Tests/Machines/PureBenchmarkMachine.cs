using Abstractions.Attributes;
using static StateMachine.Tests.Performance.BenchmarkTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
    [GenerationMode(GenerationMode.Pure, Force = true)]
    public partial class PureBenchmarkMachine
    {
        [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
        [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.C)]
        [Transition(BenchmarkState.C, BenchmarkTrigger.Next, BenchmarkState.D)]
        [Transition(BenchmarkState.D, BenchmarkTrigger.Next, BenchmarkState.A)]
        private void Configure() { }
    }
}
