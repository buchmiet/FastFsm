using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(EmptyState), typeof(EmptyTrigger))]
public partial class NoTransitionsMachineLegacy
{
    private void NoConfig() { }
}
