using Abstractions.Fluent;
using Machines.Tests.Features.Performance;

namespace Machines.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class BasicBenchmarkMachineFluent
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