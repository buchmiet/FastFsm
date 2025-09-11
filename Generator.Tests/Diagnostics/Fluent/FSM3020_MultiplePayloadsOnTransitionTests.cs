using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Fluent;

public class FSM3020_MultiplePayloadsOnTransitionTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM3020_For_Multiple_Payload_Calls()
    {
        const string src = @"
using Abstractions.Fluent;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        public static void Configure() => FSM.State(State.A)
            .On(Trigger.X)
            .Payload(typeof(string))
            .Payload(typeof(int))
            .Internal();
    }
}
";

        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.MultiplePayloadsOnTransition).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM3020 for multiple payload definitions.");
    }
}

