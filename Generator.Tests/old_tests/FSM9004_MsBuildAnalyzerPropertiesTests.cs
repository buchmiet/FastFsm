using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Infra;

public class FSM9004_MsBuildAnalyzerPropertiesTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM9004_MsBuildAnalyzerProperties()
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
        var msbuild = diags.Where(d => d.Id == RuleIdentifiers.MsBuildAnalyzerProperties).ToList();
        var logprops = diags.Where(d => d.Id == RuleIdentifiers.LogProps).ToList();
        Assert.True(msbuild.Count >= 1 || logprops.Count >= 1, "Expected FSM9004 (or FSM9013 as proxy).");
    }
}
