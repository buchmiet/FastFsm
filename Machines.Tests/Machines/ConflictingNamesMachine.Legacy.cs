using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(ConflictState), typeof(ConflictTrigger))]
public partial class ConflictingNamesMachineLegacy
{
    [Transition(ConflictState.A, ConflictTrigger.Go, ConflictState.B)]
    private void ConfigureTransitions() { }

    // User method with same name as generated (different signature)
    public string TryFire(string input) => $"User TryFire: {input}";
}