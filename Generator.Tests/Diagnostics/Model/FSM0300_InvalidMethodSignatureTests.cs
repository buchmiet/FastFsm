using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Model;

public class FSM0300_InvalidMethodSignatureTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0300_For_Invalid_Guard_Signature()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        private int BadGuard() => 1; // invalid: guards must return bool/ValueTask<bool>

        [Transition(State.A, Trigger.Go, State.B, Guard = nameof(BadGuard))]
        private void Config() { }
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidMethodSignature).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0300 diagnostic for invalid guard signature.");
    }
}

