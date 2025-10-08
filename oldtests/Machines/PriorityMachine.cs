using Abstractions.Attributes;
using System.Collections.Generic;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(PriorityMachine_S), typeof(PriorityMachine_T), EnableHierarchy = true)]
public partial class PriorityMachine
{
    public List<string> Log { get; } = new();

    [State(PriorityMachine_S.Parent)] private void Parent() { }
    [State(PriorityMachine_S.Child, Parent = PriorityMachine_S.Parent, IsInitial = true)] private void Child() { }

    [Transition(PriorityMachine_S.Parent, PriorityMachine_T.Go, PriorityMachine_S.ParentDone, Priority = 200, Action = nameof(P))]
    [Transition(PriorityMachine_S.Child, PriorityMachine_T.Go, PriorityMachine_S.Child, Priority = 100, Action = nameof(C))]
    private void Configure() { }

    private void P() => Log.Add("Parent");
    private void C() => Log.Add("Child");
}
