using Abstractions.Attributes;
﻿using Abstractions.Fluent;

namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(UnicodeState), typeof(UnicodeTrigger))]
public partial class UnicodeMachine
{
    private void Configure() => FSM
        .State(UnicodeState.αlpha)
        .On(UnicodeTrigger.βeta).GoTo(UnicodeState.Ωmega)
        .State(UnicodeState.Ωmega)
        .On(UnicodeTrigger.γamma).GoTo(UnicodeState.βeta);
}
