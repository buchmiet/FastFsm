using Xunit;
using Xunit.Abstractions;

namespace FastFsm.Tests.Features.EdgeCases;

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
        var machine = new Machines.KeywordStateMachineLegacy(NameCollisionTests.KeywordState.@class);
        machine.Start();

        // Assert - Machine works correctly with keyword names
        Assert.Equal(NameCollisionTests.KeywordState.@class, machine.CurrentState);

        Assert.True(machine.CanFire(NameCollisionTests.KeywordTrigger.@goto));
        machine.Fire(NameCollisionTests.KeywordTrigger.@goto);
        Assert.Equal(NameCollisionTests.KeywordState.@return, machine.CurrentState);

        machine.Fire(NameCollisionTests.KeywordTrigger.@continue);
        Assert.Equal(NameCollisionTests.KeywordState.@void, machine.CurrentState);

        machine.Fire(NameCollisionTests.KeywordTrigger.@break);
        Assert.Equal(NameCollisionTests.KeywordState.@int, machine.CurrentState);

        // Verify GetPermittedTriggers works
        var triggers = machine.GetPermittedTriggers();
        Assert.Contains(NameCollisionTests.KeywordTrigger.@new, triggers);
    }

    [Fact]
    public void Legacy_ReservedMethodNames_DontConflictWithGenerated()
    {
        // Test that user methods with names like TryFire don't conflict
        var machine = new Machines.ConflictingNamesMachineLegacy(NameCollisionTests.ConflictState.A);
        machine.Start();
        var typedMachine = machine as Machines.ConflictingNamesMachineLegacy;

        // User's TryFire method (different signature)
        var userResult = typedMachine!.TryFire("test");
        Assert.Equal("User TryFire: test", userResult);

        // Generated TryFire method
        var generatedResult = machine.TryFire(NameCollisionTests.ConflictTrigger.Go);
        Assert.True(generatedResult);
        Assert.Equal(NameCollisionTests.ConflictState.B, machine.CurrentState);
    }

    [Fact]
    public void Legacy_SpecialCharactersInEnumNames_HandledCorrectly()
    {
        // C# allows Unicode in identifiers
        var machine = new Machines.UnicodeMachineLegacy(NameCollisionTests.UnicodeState.αlpha);
        machine.Start();

        Assert.True(machine.TryFire(NameCollisionTests.UnicodeTrigger.βeta));
        Assert.Equal(NameCollisionTests.UnicodeState.Ωmega, machine.CurrentState);
    }

    [Fact]
    public void Legacy_VeryLongStateNames_HandledCorrectly()
    {
        // Test with extremely long enum names
        var machine = new Machines.LongNameMachineLegacy(
            NameCollisionTests.LongNameState.ThisIsAnExtremelyLongStateNameThatShouldStillWorkCorrectlyInTheGeneratedCode_Part1_Part2_Part3_Part4_Part5);
        machine.Start();

        Assert.True(machine.CanFire(
            NameCollisionTests.LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3));

        machine.Fire(
            NameCollisionTests.LongNameTrigger.ThisIsAnEquallyLongTriggerNameThatTestsTheLimitsOfNaming_Section1_Section2_Section3);

        Assert.Equal(
            NameCollisionTests.LongNameState.AnotherVeryLongStateNameForTesting_PartA_PartB_PartC_PartD_PartE_PartF,
            machine.CurrentState);
    }

    [Fact]
    public void Legacy_NumericPrefixedNames_HandledCorrectly()
    {
        // C# doesn't allow pure numeric names, but prefixed is OK
        var machine = new Machines.NumericMachineLegacy(NameCollisionTests.NumericState._1Start);
        machine.Start();

        machine.Fire(NameCollisionTests.NumericTrigger._2Next);
        Assert.Equal(NameCollisionTests.NumericState._3Middle, machine.CurrentState);

        machine.Fire(NameCollisionTests.NumericTrigger._4Continue);
        Assert.Equal(NameCollisionTests.NumericState._5End, machine.CurrentState);
    }

    [Fact]
    public void Legacy_CaseSensitiveNames_HandledCorrectly()
    {
        // Test case-sensitive enum members
        var machine = new Machines.CaseSensitiveMachineLegacy(NameCollisionTests.CaseSensitiveState.state);
        machine.Start();

        // Different cases are different states
        machine.Fire(NameCollisionTests.CaseSensitiveTrigger.GO);
        Assert.Equal(NameCollisionTests.CaseSensitiveState.STATE, machine.CurrentState);

        machine.Fire(NameCollisionTests.CaseSensitiveTrigger.go);
        Assert.Equal(NameCollisionTests.CaseSensitiveState.State, machine.CurrentState);

        machine.Fire(NameCollisionTests.CaseSensitiveTrigger.Go);
        Assert.Equal(NameCollisionTests.CaseSensitiveState.state, machine.CurrentState);
    }
}