using Abstractions.Attributes;
using Abstractions.Fluent;
using static FastFsm.Tests.Features.Performance.BenchmarkTests;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class WithGuardBenchmarkMachine
{
    private int _counter;

    private static void Configure() => FSM
        .State(BenchmarkState.A)
        .On(BenchmarkTrigger.Next).Guard(nameof(CanTransition)).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
        .On(BenchmarkTrigger.Next).Guard(nameof(CanTransition)).GoTo(BenchmarkState.A);

    private bool CanTransition()
    {
        _counter++;
        return _counter % 2 == 0; // Simple condition
    }
}