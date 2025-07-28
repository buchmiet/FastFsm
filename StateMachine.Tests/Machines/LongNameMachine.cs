using Abstractions.Attributes;
using static StateMachine.Tests.EdgeCases.NameCollisionTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(LongNameState), typeof(LongNameTrigger))]
    public partial class LongNameMachine
    {
        [Transition(
            LongNameState.ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5,
            LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3,
            LongNameState.AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF)]
        private void Configure() { }
    }
}
