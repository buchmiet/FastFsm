using Abstractions.Fluent;
using FastFsm.Tests.Features.EdgeCases;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(LongNameState), typeof(LongNameTrigger))]
public partial class LongNameMachineFluent
{
    private void Configure() => FSM
        .State(LongNameState.ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5)
        .On(LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3)
        .GoTo(LongNameState.AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF);
}