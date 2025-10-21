using Abstractions.Attributes;
using Abstractions.Fluent;

namespace Example.Tests.Unreachable.Fluent;

[StateMachine(typeof(UnreachableState), typeof(UnreachableTrigger))]
public partial class UnreachableMachineFluent
{
    private void Configure() => FSM
        .State(UnreachableState.Start).IsInitial()
            .On(UnreachableTrigger.Connect).GoTo(UnreachableState.Connected)
        .State(UnreachableState.Connected)
        .State(UnreachableState.Isolated);
}
