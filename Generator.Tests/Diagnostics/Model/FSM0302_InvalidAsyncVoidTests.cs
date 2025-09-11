using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Generator.Tests.Diagnostics.Model;

public class FSM0302_InvalidAsyncVoidTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM0302_When_Action_Is_AsyncVoid()
    {
        const string src = @"
using System.Threading.Tasks;
using Abstractions.Attributes;
namespace Test {
    public enum State { A, B }
    public enum Trigger { Go }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        private async void BadAction() { await Task.Delay(1); }

        [Transition(State.A, Trigger.Go, State.B, Action = nameof(BadAction))]
        private void Config() { }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidAsyncVoid).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM0302 diagnostic for async void callback.");
    }
}

