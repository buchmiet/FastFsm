using Abstractions.Fluent;
using Machines.Tests.Features.Performance;

namespace Machines.Tests.Machines;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class WithGuardBenchmarkMachineFluent
{
    private int _counter;
    public bool ShouldAllow { get; set; } = true;

    private void Configure() => FSM
        .State<BenchmarkState>(BenchmarkState.A)
        .On(BenchmarkTrigger.Next).Guard(CanTransition).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
        .On(BenchmarkTrigger.Next).Guard(CanTransition).GoTo(BenchmarkState.A)
        .State(BenchmarkState.C)
        .State(BenchmarkState.D);

    private bool CanTransition()
    {
        return ShouldAllow; // Simple condition based on ShouldAllow flag
    }
}