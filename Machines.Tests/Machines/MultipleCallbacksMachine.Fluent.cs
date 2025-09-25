using Abstractions.Fluent;
using Machines.Tests.Features.Core;

namespace Machines.Tests.Machines;

[StateMachine(typeof(MultiState), typeof(MultiTrigger))]
public partial class MultipleCallbacksMachineFluent
{
    public List<string> Log { get; } = [];

    private void Configure() => FSM
        .State<MultiState>(MultiState.A)
        .OnEntry(nameof(OnEntry1))
        .OnEntry(nameof(OnEntry2))
        .On(MultiTrigger.Go).GoTo(MultiState.B);

    private void OnEntry1() => Log.Add("Entry1");
    private void OnEntry2() => Log.Add("Entry2");
}