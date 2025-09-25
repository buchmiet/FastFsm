using Abstractions.Fluent;
using Machines.Tests.Features.Performance;

namespace Machines.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class NoGuardBenchmarkMachineFluent
{
    private void Configure() => FSM
        .State<BenchmarkState>(BenchmarkState.A)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
            .On(BenchmarkTrigger.Next).GoTo(BenchmarkState.A)
        .State(BenchmarkState.C)
        .State(BenchmarkState.D);
}