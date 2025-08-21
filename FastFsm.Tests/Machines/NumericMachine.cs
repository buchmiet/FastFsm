using Abstractions.Attributes;
using static FastFsm.Tests.Features.EdgeCases.NameCollisionTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(NumericState), typeof(NumericTrigger))]
    public partial class NumericMachine
    {
        [Transition(NumericState._1Start, NumericTrigger._2Next, NumericState._3Middle)]
        [Transition(NumericState._3Middle, NumericTrigger._4Continue, NumericState._5End)]
        private void Configure() { }
    }
}
