using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Model;

public class FSM0500_UnreachableStateTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0500_For_Unreachable_State()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { Start, Mid, Unreach }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        // No [State] attributes on purpose -> fallback uses all enum members
        [Transition(State.Start, Trigger.Go, State.Mid)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.UnreachableState).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0500 for at least one unreachable state (Unreach).");
    }
}

