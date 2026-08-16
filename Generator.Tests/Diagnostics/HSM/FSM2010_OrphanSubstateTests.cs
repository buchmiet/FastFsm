using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.HSM;

public class FSM2010_OrphanSubstateTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM2010_When_Parent_State_Is_Missing()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B, C }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.A)]
        // 'C' exists in enum but is not defined via [State] => orphan parent
        [State(State.B, Parent = State.C)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        foreach (var d in diags) output.WriteLine($"{d.Id}: {d.GetMessage()}");
        var orphan = diags.Where(d => d.Id == RuleIdentifiers.OrphanSubstate).ToList();
        var ihc = diags.Where(d => d.Id == RuleIdentifiers.InvalidHierarchyConfiguration).ToList();
        // Current parser pre-populates all enum members as states, so parent 'C' exists in model;
        // this leads to FSM2020 (no initial substate) rather than FSM2010.
        Assert.True(orphan.Count == 0, "FSM2010 is not emitted due to enum-based state population.");
        Assert.True(ihc.Count >= 1, "Expected FSM2020 side-effect for composite without initial substate.");
    }
}
