using System.Collections.Generic;
using Abstractions.Fluent;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
public partial class MultipleCallbacksMachineFluent
{
    public List<string> Log { get; } = [];

    private void Configure() => FSM
        .State<MultiState>(MultiState.Initial)
        .OnEntry(nameof(OnEntry1))
        .OnEntry(nameof(OnEntry2))
        .On(MultiTrigger.Process).GoTo(MultiState.Configured);

    private void OnEntry1() => Log.Add("Entry1");
    private void OnEntry2() => Log.Add("Entry2");
}