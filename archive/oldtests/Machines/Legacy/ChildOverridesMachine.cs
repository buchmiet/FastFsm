using Abstractions.Attributes;
using System.Collections.Generic;

namespace FastFsm.Tests.Machines.Legacy;

[StateMachine(typeof(ChildOverridesMachine_S), typeof(ChildOverridesMachine_T), EnableHierarchy = true)]
public partial class ChildOverridesMachine
{
    public List<string> Log { get; } = new();

    [State(ChildOverridesMachine_S.Parent)] private void Parent() { }
    [State(ChildOverridesMachine_S.Child, Parent = ChildOverridesMachine_S.Parent, IsInitial = true)] private void Child() { }

    [Transition(ChildOverridesMachine_S.Parent, ChildOverridesMachine_T.Go, ChildOverridesMachine_S.Parent, Priority = 100, Action = nameof(P))]
    [Transition(ChildOverridesMachine_S.Child, ChildOverridesMachine_T.Go, ChildOverridesMachine_S.Child, Priority = 100, Action = nameof(C))]
    private void Configure() { }

    private void P() => Log.Add("Parent");
    private void C() => Log.Add("Child");
}
