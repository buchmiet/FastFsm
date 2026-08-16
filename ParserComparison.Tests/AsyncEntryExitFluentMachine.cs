using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

[StateMachine(typeof(AeefState), typeof(AeefTrigger))]
public partial class AsyncEntryExitFluentMachine
{
    public enum AeefState { A, B }
    public enum AeefTrigger { Go }

    private static void Configure() => FSM
        .State(AeefState.A).OnEntry(nameof(OnAEntryAsync)).OnExit(nameof(OnAExitAsync))
        .State(AeefState.B).OnEntry(nameof(OnBEntryAsync)).OnExit(nameof(OnBExitAsync))
        .State(AeefState.A).On(AeefTrigger.Go).GoTo(AeefState.B);

    private async ValueTask OnAEntryAsync() { await Task.Yield(); }
    private async ValueTask OnAExitAsync() { await Task.Yield(); }
    private async ValueTask OnBEntryAsync() { await Task.Yield(); }
    private async ValueTask OnBExitAsync() { await Task.Yield(); }
}

