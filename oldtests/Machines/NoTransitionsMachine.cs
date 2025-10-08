using FastFsm.Tests.Features.EdgeCases;
using Abstractions.Attributes;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachine
{
    // No transitions defined
    private void NoConfig() { }
}