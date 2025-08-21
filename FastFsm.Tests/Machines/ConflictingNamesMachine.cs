using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(ConflictState), typeof(ConflictTrigger))]
    public partial class ConflictingNamesMachine
    {
        [Transition(ConflictState.A, ConflictTrigger.Go, ConflictState.B)]
        private void Configure() { }

        // User method with same name as generated (different signature)
        public string TryFire(string input) => $"User TryFire: {input}";
    }
}
