using Abstractions.Fluent;
using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachineFluent
{
    private void Configure() => FSM
        .State(EmptyState.Only);
}
