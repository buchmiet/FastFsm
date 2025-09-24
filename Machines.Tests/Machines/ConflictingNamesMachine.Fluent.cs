using Abstractions.Fluent;
using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(ConflictState), typeof(ConflictTrigger))]
public partial class ConflictingNamesMachineFluent
{
    private void Configure() => FSM
        .State(ConflictState.A)
        .On(ConflictTrigger.Go).GoTo(ConflictState.B);

    // User method with same name as generated (different signature)
    public string TryFire(string input) => $"User TryFire: {input}";
}