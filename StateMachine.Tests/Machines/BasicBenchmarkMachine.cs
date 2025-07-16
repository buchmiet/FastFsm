using Abstractions.Attributes;
using static StateMachine.Tests.Performance.BenchmarkTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
    public partial class BasicBenchmarkMachine
    {
        private int _counter;

        [State(BenchmarkState.A, OnEntry = nameof(IncrementCounter))]
        [State(BenchmarkState.B, OnEntry = nameof(IncrementCounter))]
        [State(BenchmarkState.C, OnEntry = nameof(IncrementCounter))]
        [State(BenchmarkState.D, OnEntry = nameof(IncrementCounter))]
        private void ConfigureStates() { }

        [Transition(BenchmarkState.A, BenchmarkTrigger.Next, BenchmarkState.B)]
        [Transition(BenchmarkState.B, BenchmarkTrigger.Next, BenchmarkState.C)]
        [Transition(BenchmarkState.C, BenchmarkTrigger.Next, BenchmarkState.D)]
        [Transition(BenchmarkState.D, BenchmarkTrigger.Next, BenchmarkState.A)]
        private void Configure() { }

        private void IncrementCounter() => _counter++;
    }
}
