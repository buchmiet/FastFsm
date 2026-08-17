using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Fluent;

public class FSM3000_OpenTransitionTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3000_For_Open_Transition_At_End_Of_Chain()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X, Y }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A).On(Trigger.X);
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.OpenTransition).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3000 for open transition.");
    }
}

