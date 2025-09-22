using Abstractions.Fluent;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(UnicodeState), typeof(UnicodeTrigger))]
public partial class UnicodeMachineFluent
{
    private void Configure() => FSM
        .State(UnicodeState.αlpha)
        .On(UnicodeTrigger.βeta).GoTo(UnicodeState.Ωmega)
        .State(UnicodeState.Ωmega)
        .On(UnicodeTrigger.γamma).GoTo(UnicodeState.βeta);
}