using Abstractions.Fluent;
using Machines.Tests.Features.EdgeCases;

namespace Machines.Tests.Machines;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachineFluent
{
    private void Configure() => FSM
        .State(EmptyState.Only);
}
