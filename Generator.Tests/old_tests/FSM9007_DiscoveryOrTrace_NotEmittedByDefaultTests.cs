using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Infra;

public class FSM9007_DiscoveryOrTrace_NotEmittedByDefaultTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void DoesNotEmit_FSM9007_ByDefault_For_Normal_Class()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test.InfraNE3 {
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
        var hits = diags.Where(d => d.Id == RuleIdentifiers.DiscoveryOrTrace).ToList();
        Assert.True(hits.Count == 0, "FSM9007 is not expected for normal class names.");
    }
}

