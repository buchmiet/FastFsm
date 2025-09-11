using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Infra;

public class FSM9003_EnumOnlyStatesFallbackTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM9003_When_No_State_Attributes_Defined()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test.Infra5 {
    public enum State { A, B }
    public enum Trigger { X }
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        // No [State] attributes -> fallback should engage
        [Transition(State.A, Trigger.X, State.B)]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.EnumOnlyStatesFallback).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM9003 Enum-only states fallback.");
    }
}

