using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class ExampleStateMachine
{
    private void Configure() => FSM
        .State(OrderState.New).On(OrderTrigger.Submit).GoTo(OrderState.Submitted);
}