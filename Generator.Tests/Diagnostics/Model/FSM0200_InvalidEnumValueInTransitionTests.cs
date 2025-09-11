using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Model;

public class FSM0200_InvalidEnumValueInTransitionTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0200_For_Invalid_Enum_Value()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State : byte { Low = 0, High = 255 }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition((State)0, Trigger.Go, (State)255)] // valid
        [Transition((State)128, Trigger.Go, State.Low)] // invalid value 128 not defined
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidEnumValueInTransition).ToList();
        Assert.True(hits.Count == 1, "Expected exactly one FSM0200 diagnostic.");
        Assert.Contains("128", hits[0].GetMessage());
    }
}

