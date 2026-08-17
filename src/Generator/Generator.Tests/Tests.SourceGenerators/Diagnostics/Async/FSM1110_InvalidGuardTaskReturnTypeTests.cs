using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Async;

public class FSM1110_InvalidGuardTaskReturnTypeTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM1110_When_Async_Guard_Returns_TaskBool_Instead_Of_ValueTaskBool()
    {
        const string src = @"
using Abstractions.Attributes;
using System.Threading.Tasks;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [Transition(State.A, Trigger.X, State.B, Guard = nameof(AsyncGuard))]
        private void Config() { }

        private async Task<bool> AsyncGuard() { await Task.Delay(1); return true; }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.InvalidGuardTaskReturnType).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM1110 for Task<bool> guard.");
    }
}

