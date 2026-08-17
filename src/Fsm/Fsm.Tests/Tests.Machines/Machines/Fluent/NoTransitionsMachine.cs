using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachine
{
    private void Configure() => FSM
        .State(EmptyState.Only);
}
