using System.Linq;
using Abstractions.Attributes;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.HSM;

public class FSM2040_InvalidHistoryConfigurationTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM2040_When_History_On_NonComposite()
    {
        const string src = @"
using Abstractions.Attributes;
namespace Test {
    public enum State { Lone }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger), EnableHierarchy = true)]
    public partial class Machine {
        [State(State.Lone, History = HistoryMode.Shallow)] // no children -> non-composite
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidHistoryConfiguration).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM2040 for history on non-composite state.");
    }
}

