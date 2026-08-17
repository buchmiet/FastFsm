using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachine
{
    private void Configure() => FSM
        .State<BenchmarkState>(BenchmarkState.A)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A)
        .State(BenchmarkState.C)
        .State(BenchmarkState.D);
}