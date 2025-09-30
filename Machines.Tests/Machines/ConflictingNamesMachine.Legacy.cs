using Abstractions.Attributes;
using Machines.Tests.Features.EdgeCases;

namespace Machines.Tests.Machines;

[StateMachine(typeof(ConflictState), typeof(ConflictTrigger))]
public partial class ConflictingNamesMachineLegacy
{
    [Transition(ConflictState.A, ConflictTrigger.Go, ConflictState.B)]
    private void ConfigureTransitions() { }

    // User method with same name as generated (different signature)
    public string TryFire(string input) => $"User TryFire: {input}";
}