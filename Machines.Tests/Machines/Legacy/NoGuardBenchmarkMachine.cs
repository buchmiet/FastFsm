using Abstractions.Fluent;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachine
{
    private void Configure() => FSM
        .State(BenchmarkState.A)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A);
}

