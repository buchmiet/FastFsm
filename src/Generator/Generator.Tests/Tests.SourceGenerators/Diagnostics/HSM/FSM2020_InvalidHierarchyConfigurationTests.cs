using System.Linq;
using Abstractions.Attributes;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.HSM;

public class FSM2020_InvalidHierarchyConfigurationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM2020_For_Composite_Without_Initial_Substate()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { Parent, Child1, Child2 }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.Parent)]
        [State(State.Child1, Parent = State.Parent)]
        [State(State.Child2, Parent = State.Parent)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidHierarchyConfiguration).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM2020 for composite without initial substate.");
    }
}

