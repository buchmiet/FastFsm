using Abstractions.Fluent;
using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(NumericState), typeof(NumericTrigger))]
public partial class NumericMachineFluent
{
    private void Configure() => FSM
        .State(NumericState._1Start)
        .On(NumericTrigger._2Next).GoTo(NumericState._3Middle)
        .State(NumericState._3Middle)
        .On(NumericTrigger._4Continue).GoTo(NumericState._5End);
}