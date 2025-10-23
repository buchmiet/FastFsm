using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

[StateMachine(typeof(StructState), typeof(StructTrigger))]
public partial class StructStateMachineFluent
{
    private void Configure() => FSM
        .State(StructState.One).On(StructTrigger.Next).GoTo(StructState.Two)
        .State(StructState.Two).On(StructTrigger.Next).GoTo(StructState.Three);
}