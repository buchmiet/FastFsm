using Abstractions.Attributes;
﻿using Abstractions.Fluent;

namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(InternalOnlyState), typeof(InternalOnlyTrigger))]
public partial class InternalOnlyMachine
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    private void Configure() => FSM
        .State(InternalOnlyState.Static)
        .OnInternal(InternalOnlyTrigger.Action).Action(nameof(PerformAction));

    private void PerformAction() => _actionCount++;
}
