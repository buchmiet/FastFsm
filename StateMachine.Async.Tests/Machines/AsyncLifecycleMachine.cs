using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;

namespace StateMachine.Async.Tests.Machines;

public enum AsyncState { A, B, C }
public enum AsyncTrigger { Go, Next }

[StateMachine(typeof(AsyncState), typeof(AsyncTrigger))]
public partial class AsyncLifecycleMachine
{
    public List<string> Log { get; } = new();

    // --- State Callbacks ---
    [State(AsyncState.A, OnExit = nameof(OnExitAAsync))]
    [State(AsyncState.B, OnEntry = nameof(OnEntryBAsync))]
    private void ConfigureStates() { }

    // --- Transitions ---
    [Transition(AsyncState.A, AsyncTrigger.Go, AsyncState.B, Action = nameof(ActionAtoBAsync))]
    private void ConfigureTransitions() { }

    // --- Async Callback Implementations ---
    private async Task OnExitAAsync()
    {
        await Task.Delay(1); // Symulacja I/O
        Log.Add("ExitA");
    }

    private async Task ActionAtoBAsync()
    {
        await Task.Delay(1);
        Log.Add("ActionAtoB");
    }

    private async Task OnEntryBAsync()
    {
        await Task.Delay(1);
        Log.Add("EntryB");
    }
}