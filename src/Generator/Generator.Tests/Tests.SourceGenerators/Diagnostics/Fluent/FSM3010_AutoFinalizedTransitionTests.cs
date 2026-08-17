using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Fluent;

public class FSM3010_AutoFinalizedTransitionTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3010_When_New_On_AutoFinalizes_Previous()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X, Y }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A).On(Trigger.X).On(Trigger.Y).GoTo(State.A);
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AutoFinalizedTransition).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3010 for auto-finalized transition.");
    }
}

