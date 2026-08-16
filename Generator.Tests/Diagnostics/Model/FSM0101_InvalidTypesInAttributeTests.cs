using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Model;

public class FSM0101_InvalidTypesInAttributeTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0101_When_StateType_Is_Not_Enum()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum Trigger { Go }
    [StateMachine(typeof(int), typeof(Trigger))]
    public partial class Machine {
        [Transition(0, Trigger.Go, 0)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidTypesInAttribute).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0101 diagnostic for non-enum state type.");
    }

    [Fact]
    public void Emits_FSM0101_When_TriggerType_Is_Not_Enum()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    [StateMachine(typeof(State), typeof(string))]
    public partial class Machine {
        [Transition(State.A, 0, State.B)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidTypesInAttribute).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0101 diagnostic for non-enum trigger type.");
    }
}

