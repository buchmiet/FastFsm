using Abstractions.Attributes;
using static StateMachine.Tests.Features.Performance.BenchmarkTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
    public partial class CoreBenchmarkMachine
    {
        [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
        [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.C)]
        [Transition(BenchmarkState.C, BenchmarkTrigger.Next, BenchmarkState.D)]
        [Transition(BenchmarkState.D, BenchmarkTrigger.Next, BenchmarkState.A)]
        private void Configure() { }
    }
}
