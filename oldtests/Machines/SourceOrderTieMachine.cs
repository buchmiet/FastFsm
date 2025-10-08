using Abstractions.Attributes;
using System.Collections.Generic;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(SourceOrderTieMachine_S), typeof(SourceOrderTieMachine_T))]
public partial class SourceOrderTieMachine
{
    public List<string> Log { get; } = new();

    [Transition(SourceOrderTieMachine_S.A, SourceOrderTieMachine_T.Go, SourceOrderTieMachine_S.B, Priority = 0, Action = nameof(First))]
    [Transition(SourceOrderTieMachine_S.A, SourceOrderTieMachine_T.Go, SourceOrderTieMachine_S.C, Priority = 0, Action = nameof(Second))]
    private void Configure() { }

    private void First() => Log.Add("First");
    private void Second() => Log.Add("Second");
}
