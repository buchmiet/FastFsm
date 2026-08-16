using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Fluent;

public class FSM3040_PriorityWithoutActiveTransitionTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3040_When_Priority_Called_Without_Active_Transition()
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
            .Priority(1) // no active transition here
            .On(Trigger.X).GoTo(State.B);
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.PriorityWithoutActiveTransition).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3040 for Priority() called without active transition.");
    }
}

