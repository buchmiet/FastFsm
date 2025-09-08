using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.EdgeCases
{
    public class NameCollisionTestsLegacy
    {
        private readonly ITestOutputHelper _output;

        public NameCollisionTestsLegacy(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Legacy_StateNames_WithCSharpKeywords_AreHandledCorrectly()
        {
            // Arrange & Act
            var machine = new Machines.KeywordStateMachineLegacy(KeywordState.@class);
            machine.Start();

            // Assert - Machine works correctly with keyword names
            Assert.Equal(KeywordState.@class, machine.CurrentState);

            Assert.True(machine.CanFire(KeywordTrigger.@goto));
            machine.Fire(KeywordTrigger.@goto);
            Assert.Equal(KeywordState.@return, machine.CurrentState);

            machine.Fire(KeywordTrigger.@continue);
            Assert.Equal(KeywordState.@void, machine.CurrentState);

            machine.Fire(KeywordTrigger.@break);
            Assert.Equal(KeywordState.@int, machine.CurrentState);

            // Verify GetPermittedTriggers works
            var triggers = machine.GetPermittedTriggers();
            Assert.Contains(KeywordTrigger.@new, triggers);
        }

        [Fact]
        public void Legacy_ReservedMethodNames_DontConflictWithGenerated()
        {
            // Test that user methods with names like TryFire don't conflict
            var machine = new Machines.ConflictingNamesMachineLegacy(ConflictState.A);
            machine.Start();
            var typedMachine = machine as Machines.ConflictingNamesMachineLegacy;

            // User's TryFire method (different signature)
            var userResult = typedMachine!.TryFire("test");
            Assert.Equal("User TryFire: test", userResult);

            // Generated TryFire method
            var generatedResult = machine.TryFire(ConflictTrigger.Go);
            Assert.True(generatedResult);
            Assert.Equal(ConflictState.B, machine.CurrentState);
        }

        [Fact]
        public void Legacy_SpecialCharactersInEnumNames_HandledCorrectly()
        {
            // C# allows Unicode in identifiers
            var machine = new Machines.UnicodeMachineLegacy(UnicodeState.αlpha);
            machine.Start();

            Assert.True(machine.TryFire(UnicodeTrigger.βeta));
            Assert.Equal(UnicodeState.Ωmega, machine.CurrentState);
        }

        [Fact]
        public void Legacy_VeryLongStateNames_HandledCorrectly()
        {
            // Test with extremely long enum names
            var machine = new Machines.LongNameMachineLegacy(
                LongNameState.ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5);
            machine.Start();

            Assert.True(machine.CanFire(
                LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3));

            machine.Fire(
                LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3);

            Assert.Equal(
                LongNameState.AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF,
                machine.CurrentState);
        }

        [Fact]
        public void Legacy_NumericPrefixedNames_HandledCorrectly()
        {
            // C# doesn't allow pure numeric names, but prefixed is OK
            var machine = new Machines.NumericMachineLegacy(NumericState._1Start);
            machine.Start();

            machine.Fire(NumericTrigger._2Next);
            Assert.Equal(NumericState._3Middle, machine.CurrentState);

            machine.Fire(NumericTrigger._4Continue);
            Assert.Equal(NumericState._5End, machine.CurrentState);
        }

        [Fact]
        public void Legacy_CaseSensitiveNames_HandledCorrectly()
        {
            // Test case-sensitive enum members
            var machine = new Machines.CaseSensitiveMachineLegacy(CaseSensitiveState.state);
            machine.Start();

            // Different cases are different states
            machine.Fire(CaseSensitiveTrigger.GO);
            Assert.Equal(CaseSensitiveState.STATE, machine.CurrentState);

            machine.Fire(CaseSensitiveTrigger.go);
            Assert.Equal(CaseSensitiveState.State, machine.CurrentState);

            machine.Fire(CaseSensitiveTrigger.Go);
            Assert.Equal(CaseSensitiveState.state, machine.CurrentState);
        }
    }
}