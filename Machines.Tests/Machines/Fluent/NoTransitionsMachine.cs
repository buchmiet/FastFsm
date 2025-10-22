using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachineFluent
{
    private void Configure() => FSM
        .State(EmptyState.Only);
}
