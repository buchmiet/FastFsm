using Abstractions.Fluent;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class BasicBenchmarkMachine
{
    private int _counter;

    private void Configure() => FSM
        .State<BenchmarkState>(BenchmarkState.A)
        .OnEntry(nameof(IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
        .OnEntry(nameof(IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.C)
        .State(BenchmarkState.C)
        .OnEntry(nameof(IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.D)
        .State(BenchmarkState.D)
        .OnEntry(nameof(IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A);

    private void IncrementCounter() => _counter++;
}