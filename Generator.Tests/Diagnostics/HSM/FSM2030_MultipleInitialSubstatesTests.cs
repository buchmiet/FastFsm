using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.HSM;

public class FSM2030_MultipleInitialSubstatesTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM2030_When_Multiple_Children_Are_Marked_Initial()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { Parent, C1, C2 }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.Parent)]
        [State(State.C1, Parent = State.Parent, IsInitial = true)]
        [State(State.C2, Parent = State.Parent, IsInitial = true)] // multiple initial
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.MultipleInitialSubstates).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM2030 for multiple initial substates.");
    }
}

