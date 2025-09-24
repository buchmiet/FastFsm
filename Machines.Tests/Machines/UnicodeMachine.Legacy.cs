using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(UnicodeState), typeof(UnicodeTrigger))]
public partial class UnicodeMachineLegacy
{
    [Transition(UnicodeState.αlpha, UnicodeTrigger.βeta, UnicodeState.Ωmega)]
    [Transition(UnicodeState.Ωmega, UnicodeTrigger.γamma, UnicodeState.βeta)]
    private void ConfigureTransitions() { }
}