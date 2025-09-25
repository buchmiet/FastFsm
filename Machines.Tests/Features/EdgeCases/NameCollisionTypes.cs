namespace Machines.Tests.Features.EdgeCases;

// C# Keyword names
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
