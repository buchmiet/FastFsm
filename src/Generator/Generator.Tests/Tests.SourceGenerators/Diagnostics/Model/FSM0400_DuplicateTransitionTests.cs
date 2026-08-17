using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Model;

public class FSM0400_DuplicateTransitionTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0400_For_Duplicate_Transition_On_Same_From_And_Trigger()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition(State.A, Trigger.X, State.B)]
        [Transition(State.A, Trigger.X, State.B)] // duplicate
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.DuplicateTransition).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0400 for duplicate transition.");
    }
}

