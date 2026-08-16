using Abstractions.Fluent;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachine
{
    private void Configure() => FSM
        .State(EmptyState.Only);
}
