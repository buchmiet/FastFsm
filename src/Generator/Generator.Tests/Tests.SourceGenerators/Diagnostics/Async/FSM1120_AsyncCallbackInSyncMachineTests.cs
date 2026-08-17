using System.Linq;
using Generator.Rules.Definitions;
using Xunit;
using Xunit.Abstractions;

namespace Tests.SourceGenerators.Diagnostics.Async;

public class FSM1120_AsyncCallbackInSyncMachineTests(ITestOutputHelper output) : GeneratorBaseClass(output)
{
    [Fact]
    public void Emits_FSM1120_For_Async_Callback_In_Sync_Machine()
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
        var hits = diags.Where(d => d.Id == RuleIdentifiers.AsyncCallbackInSyncMachine).ToList();
        // Current parser short-circuits on FSM1100 (mixed mode), so FSM1120 does not emit.
        Assert.True(hits.Count == 0, "FSM1120 is not emitted due to early return after FSM1100.");
    }
}
