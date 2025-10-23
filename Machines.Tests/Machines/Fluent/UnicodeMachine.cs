using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(UnicodeState), typeof(UnicodeTrigger))]
public partial class UnicodeMachine
{
    private void Configure() => FSM
        .State(UnicodeState.αlpha)
        .On(UnicodeTrigger.βeta).GoTo(UnicodeState.Ωmega)
        .State(UnicodeState.Ωmega)
        .On(UnicodeTrigger.γamma).GoTo(UnicodeState.βeta);
}