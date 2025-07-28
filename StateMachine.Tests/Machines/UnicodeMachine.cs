using Abstractions.Attributes;
using static StateMachine.Tests.EdgeCases.NameCollisionTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(UnicodeState), typeof(UnicodeTrigger))]
    public partial class UnicodeMachine
    {
        [Transition(UnicodeState.αlpha, UnicodeTrigger.βeta, UnicodeState.Ωmega)]
        [Transition(UnicodeState.Ωmega, UnicodeTrigger.γamma, UnicodeState.βeta)]
        private void Configure() { }
    }
}
