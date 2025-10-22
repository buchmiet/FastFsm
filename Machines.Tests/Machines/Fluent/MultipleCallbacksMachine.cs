using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
public partial class MultipleCallbacksMachineFluent
{
    public List<string> Log { get; } = [];

    private void Configure() => FSM
        .State<MultiState>(MultiState.Initial)
        .OnEntry((OnEntry1))
        .OnEntry((OnEntry2))
        .On(MultiTrigger.Process).GoTo(MultiState.Configured);

    private void OnEntry1() => Log.Add("Entry1");
    private void OnEntry2() => Log.Add("Entry2");
}