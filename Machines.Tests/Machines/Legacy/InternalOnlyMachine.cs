using Abstractions.Attributes;
﻿using Abstractions.Fluent;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(InternalOnlyState), typeof(InternalOnlyTrigger))]
public partial class InternalOnlyMachine
{
    private int _actionCount;
    public int ActionCount => _actionCount;

    private static void Configure() => FSM
        .State(InternalOnlyState.Static)
        .OnInternal(InternalOnlyTrigger.Action).Action((PerformAction));

    private void PerformAction() => _actionCount++;
}
