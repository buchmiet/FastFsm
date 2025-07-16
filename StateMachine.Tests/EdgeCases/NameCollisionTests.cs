using Xunit;
using Xunit.Abstractions;

namespace StateMachine.Tests.EdgeCases
{
    public class NameCollisionTests
    {
        private readonly ITestOutputHelper _output;

        public NameCollisionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void StateNames_WithCSharpKeywords_AreHandledCorrectly()
        {
            // Arrange & Act
            var machine = new Machines.KeywordStateMachine(KeywordState.@class);

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
        public void ReservedMethodNames_DontConflictWithGenerated()
        {
            // Test that user methods with names like TryFire don't conflict
            var machine = new Machines.ConflictingNamesMachine(ConflictState.A);
            var typedMachine = machine as Machines.ConflictingNamesMachine;

            // User's TryFire method (different signature)
            var userResult = typedMachine!.TryFire("test");
            Assert.Equal("User TryFire: test", userResult);

            // Generated TryFire method
            var generatedResult = machine.TryFire(ConflictTrigger.Go);
            Assert.True(generatedResult);
            Assert.Equal(ConflictState.B, machine.CurrentState);
        }

        [Fact]
        public void SpecialCharactersInEnumNames_HandledCorrectly()
        {
            // C# allows Unicode in identifiers
            var machine = new Machines.UnicodeMachine(UnicodeState.αlpha);

            Assert.True(machine.TryFire(UnicodeTrigger.βeta));
            Assert.Equal(UnicodeState.Ωmega, machine.CurrentState);
        }

        [Fact]
        public void VeryLongStateNames_HandledCorrectly()
        {
            // Test with extremely long enum names
            var machine = new Machines.LongNameMachine(
                LongNameState.ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5);

            Assert.True(machine.CanFire(
                LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3));

            machine.Fire(
                LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3);

            Assert.Equal(
                LongNameState.AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF,
                machine.CurrentState);
        }

        [Fact]
        public void NumericPrefixedNames_HandledCorrectly()
        {
            // C# doesn't allow pure numeric names, but prefixed is OK
            var machine = new Machines.NumericMachine(NumericState._1Start);

            machine.Fire(NumericTrigger._2Next);
            Assert.Equal(NumericState._3Middle, machine.CurrentState);

            machine.Fire(NumericTrigger._4Continue);
            Assert.Equal(NumericState._5End, machine.CurrentState);
        }

        [Fact]
        public void CaseSensitiveNames_HandledCorrectly()
        {
            // Test case-sensitive enum members
            var machine = new Machines.CaseSensitiveMachine(CaseSensitiveState.state);

            // Different cases are different states
            machine.Fire(CaseSensitiveTrigger.GO);
            Assert.Equal(CaseSensitiveState.STATE, machine.CurrentState);

            machine.Fire(CaseSensitiveTrigger.go);
            Assert.Equal(CaseSensitiveState.State, machine.CurrentState);

            machine.Fire(CaseSensitiveTrigger.Go);
            Assert.Equal(CaseSensitiveState.state, machine.CurrentState);
        }

        // Test state machines with naming edge cases

        // C# Keywords as names
        public enum KeywordState
        {
            @class,
            @return,
            @void,
            @int,
            @interface,
            @namespace
        }

        public enum KeywordTrigger
        {
            @goto,
            @continue,
            @break,
            @new,
            @throw
        }

   

        // Conflicting method names
        public enum ConflictState { A, B }
        public enum ConflictTrigger { Go }



        // Unicode names
        public enum UnicodeState
        {
            αlpha,
            βeta,
            Ωmega
        }

        public enum UnicodeTrigger
        {
            αlpha,
            βeta,
            γamma
        }



        // Very long names
        public enum LongNameState
        {
            ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5,
            AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF
        }

        public enum LongNameTrigger
        {
            ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3
        }

  

        // Numeric prefixed names
        public enum NumericState
        {
            _1Start,
            _3Middle,
            _5End
        }

        public enum NumericTrigger
        {
            _2Next,
            _4Continue
        }



        // Case sensitive names
        public enum CaseSensitiveState
        {
            state,
            State,
            STATE
        }

        public enum CaseSensitiveTrigger
        {
            go,
            Go,
            GO
        }

      
    }
}