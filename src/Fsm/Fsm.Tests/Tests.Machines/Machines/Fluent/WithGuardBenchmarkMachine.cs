using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(BenchmarkState), typeof(BenchmarkTrigger))]
public partial class WithGuardBenchmarkMachine
{
    public bool ShouldAllow { get; set; } = true;

    private void Configure() => FSM
        .State<BenchmarkState>(BenchmarkState.A)
        .On(BenchmarkTrigger.Next).Guard(CanTransition).GoTo(BenchmarkState.B)
        .State(BenchmarkState.B)
        .On(BenchmarkTrigger.Next).Guard(CanTransition).GoTo(BenchmarkState.A)
        .State(BenchmarkState.C)
        .State(BenchmarkState.D);

    private bool CanTransition() => ShouldAllow; // Simple condition based on ShouldAllow flag
}