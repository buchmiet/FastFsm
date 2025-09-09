using Abstractions.Attributes;
using FastFsm.Tests.Features.Performance;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(BenchmarkTestsLegacy.BenchmarkState), typeof(BenchmarkTestsLegacy.BenchmarkTrigger))]
    public partial class BasicBenchmarkMachineLegacy
    {
        private int _counter;

        [State(BenchmarkTestsLegacy.BenchmarkState.A, OnEntry = nameof(IncrementCounter))]
        [State(BenchmarkTestsLegacy.BenchmarkState.B, OnEntry = nameof(IncrementCounter))]
        [State(BenchmarkTestsLegacy.BenchmarkState.C, OnEntry = nameof(IncrementCounter))]
        [State(BenchmarkTestsLegacy.BenchmarkState.D, OnEntry = nameof(IncrementCounter))]
        private void ConfigureStates() { }

        [Transition(BenchmarkTestsLegacy.BenchmarkState.A, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.B)]
        [Transition(BenchmarkTestsLegacy.BenchmarkState.B, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.C)]
        [Transition(BenchmarkTestsLegacy.BenchmarkState.C, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.D)]
        [Transition(BenchmarkTestsLegacy.BenchmarkState.D, BenchmarkTestsLegacy.BenchmarkTrigger.Next, BenchmarkTestsLegacy.BenchmarkState.A)]
        private void Configure() { }

        private void IncrementCounter() => _counter++;
    }
}
