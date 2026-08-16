using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.HSM;

public class FSM2000_CircularHierarchyTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM2000_For_Circular_Hierarchy()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B, C }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.A, Parent = State.B)]
        [State(State.B, Parent = State.C)]
        [State(State.C, Parent = State.A)] // circular
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.CircularHierarchy).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM2000 for circular hierarchy.");
    }
}

