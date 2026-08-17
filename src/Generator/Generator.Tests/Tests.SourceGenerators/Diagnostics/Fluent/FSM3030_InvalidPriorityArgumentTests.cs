using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Fluent;

public class FSM3030_InvalidPriorityArgumentTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3030_When_Priority_Not_Int_Literal()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .On(Trigger.X)
            .Priority(1 + 2) // not a literal
            .GoTo(State.B);
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidPriorityArgument).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3030 for non-literal priority argument.");
    }
}

