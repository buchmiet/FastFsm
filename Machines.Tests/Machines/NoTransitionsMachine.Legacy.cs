using Machines.Tests.Features.EdgeCases;

namespace Machines.Tests.Machines;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachineLegacy
{
    private void NoConfig() { }
}
