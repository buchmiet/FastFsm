using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Tests.Features.Exceptions
{
    /// <summary>
    /// Fluent API version of TestMachine
    /// </summary>
    [StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
    public partial class TestMachineFluent
    {
        private static void Configure() => FSM
            .State(State.Initial)
                .On(Trigger.Next).GoTo(State.Final)
            .State(State.Final);
    }
}