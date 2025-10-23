using Abstractions.Fluent;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class TestMachineFluentAPI
{
    private void Configure() => FSM
        .State(State.Initial)
        .On(Trigger.Next).GoTo(State.Final)
        .State(State.Final);
}
