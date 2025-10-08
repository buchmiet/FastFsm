using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Exceptions;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
public partial class TestMachineFluentAPI
{
    private static void Configure() => FSM
        .State(State.Initial)
        .On(Trigger.Next).GoTo(State.Final)
        .State(State.Final);
}