using Abstractions.Fluent;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class BasicBenchmarkMachineFluentAPI
{
    private int _counter;

    private void Configure() => FSM
        .State(BenchmarkState.A)
        .OnEntry((IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
        .OnEntry((IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.C)
        .State(BenchmarkState.C)
        .OnEntry((IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.D)
        .State(BenchmarkState.D)
        .OnEntry((IncrementCounter))
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A);

    private void IncrementCounter() => _counter++;
}