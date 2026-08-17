using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Async;

public class FSM1100_MixedSyncAsyncCallbacksTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM1100_When_Mixing_Sync_And_Async_Callbacks()
    {
        const string src = @"
using Abstractions.Attributes;
using System.Threading.Tasks;
namespace Test {
    public enum State { A, B }
    public enum Trigger { X }

    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class Machine {
        [State(State.A, OnEntry = nameof(SyncEntry))]
        [State(State.B, OnEntry = nameof(AsyncEntry))]
        private void Config() { }

        private void SyncEntry() { }
        private async Task AsyncEntry() { await Task.Delay(1); }
    }
}
";
        var (_, diags, _) = CompileAndRunGenerator([src], new StateMachineGenerator());
        var hits = diags.Where(d => d.Id == RuleIdentifiers.MixedSyncAsyncCallbacks).ToList();
        Assert.True(hits.Count >= 1, "Expected FSM1100 for mixing sync/async callbacks.");
    }
}

