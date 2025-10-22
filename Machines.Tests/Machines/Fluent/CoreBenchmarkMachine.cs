using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class CoreBenchmarkMachineFluent
{
    private void Configure() => FSM
        .State<BenchmarkState>(BenchmarkState.A)
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.C)
        .State(BenchmarkState.C)
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.D)
        .State(BenchmarkState.D)
        .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A);
}