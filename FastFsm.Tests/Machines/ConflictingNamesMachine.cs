using Abstractions.Attributes;
using Abstractions.Fluent;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(ConflictState), typeof(ConflictTrigger))]
    public partial class ConflictingNamesMachine
    {
        private static void Configure() => FSM
            .State(ConflictState.A)
                .On(ConflictTrigger.Go).GoTo(ConflictState.B);

        // User method with same name as generated (different signature)
        public string TryFire(string input) => $"User TryFire: {input}";
    }
}
