using Abstractions.Fluent;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(BasicState), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class TestMachineFluentAPI
{
    private void Configure() => FSM
        .State(BasicState.Initial)
        .On(Trigger.Next).GoTo(BasicState.Final)
        .State(BasicState.Final);
}
