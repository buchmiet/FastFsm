using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Model;

public class FSM0301_GuardWithPayloadInNonPayloadMachineTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0301_When_Guard_Expects_Payload_But_Machine_Has_None()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        private bool Guard(object payload) => true; // expects payload, but machine has no payload config

        [Transition(State.A, Trigger.Go, State.B, Guard = nameof(Guard))]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.GuardWithPayloadInNonPayloadMachine).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0301 diagnostic for payload guard in non-payload machine.");
    }
}

