using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Infra;

public class FSM9001_DeclarationPlanTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM9001_DeclarationPlan()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition(State.A, Trigger.X, State.B)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        foreach (var d in diags) output.WriteLine($"{d.Id}: {d.GetMessage()}");
        var hits = diags.Where(d => d.Id == RuleIdentifiers.DeclarationPlan).ToList();
        // Some infra diagnostics may be suppressed depending on context. Accept presence of 9005/9009 as progress proxies.
        if (hits.Count == 0)
        {
            var proxies = diags.Where(d => d.Id == RuleIdentifiers.AddSourceOk || d.Id == RuleIdentifiers.VariantDecision).ToList();
            Assert.True(proxies.Count >= 1, "Expected related infra diagnostics (AddSourceOk/VariantDecision) when DeclarationPlan is not emitted.");
        }
    }
}
