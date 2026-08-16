using System.Threading.Tasks;
using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(AeesState), typeof(AeesTrigger))]
public partial class AsyncEntryExitStateMachine
{
    public enum AeesState { A, B }
    public enum AeesTrigger { Go }

    [State(AeesState.A, OnEntry = nameof(OnAEntryAsync), OnExit = nameof(OnAExitAsync))]
    [State(AeesState.B, OnEntry = nameof(OnBEntryAsync), OnExit = nameof(OnBExitAsync))]
    private void ConfigureStates() { }

    [Transition(AeesState.A, AeesTrigger.Go, AeesState.B)]
    private void Configure() { }

    private async ValueTask OnAEntryAsync() { await Task.Yield(); }
    private async ValueTask OnAExitAsync() { await Task.Yield(); }
    private async ValueTask OnBEntryAsync() { await Task.Yield(); }
    private async ValueTask OnBExitAsync() { await Task.Yield(); }
}

