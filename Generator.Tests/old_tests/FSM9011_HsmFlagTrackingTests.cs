using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Infra;

public class FSM9011_HsmFlagTrackingTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM9011_HsmFlagTracking_During_Generation()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test.Infra4 {
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
        var hits = diags.Where(d => d.Id == RuleIdentifiers.HsmFlagTracking).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM9011 HsmFlagTracking.");
    }
}

