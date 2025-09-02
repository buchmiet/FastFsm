using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

// Attribute-based version (reference implementation)
[StateMachine(typeof(AsyncState), typeof(AsyncTrigger))]
public partial class SimpleAsyncMachine
{
    [State(AsyncState.Idle, OnEntry = nameof(OnEnterIdleAsync))]
    private void ConfigureIdleState() { }
    
    [State(AsyncState.Working, OnExit = nameof(OnExitWorkingAsync))]
    private void ConfigureWorkingState() { }
    
    [State(AsyncState.Complete)]
    private void ConfigureCompleteState() { }
    
    [Transition(AsyncState.Idle, AsyncTrigger.Start, AsyncState.Working, 
        Guard = nameof(CanStartAsync), Action = nameof(StartWorkAsync))]
    [Transition(AsyncState.Working, AsyncTrigger.Finish, AsyncState.Complete)]
    [Transition(AsyncState.Complete, AsyncTrigger.Reset, AsyncState.Idle)]
    private void ConfigureTransitions() { }
    
    private async ValueTask<bool> CanStartAsync()
    {
        await Task.Yield();
        return true;
    }

    private async Task StartWorkAsync()
    {
        await Task.Yield();
    }

    private async Task OnEnterIdleAsync()
    {
        await Task.Yield();
    }

    private async Task OnExitWorkingAsync()
    {
        await Task.Yield();
    }
}

// Fluent API version (should generate same model)
[StateMachine(typeof(AsyncState), typeof(AsyncTrigger))]
public partial class SimpleAsyncMachineFluentFsm
{
    private static void Configure() => FSM
        .State(AsyncState.Idle)
            .OnEntryAsync(nameof(OnEnterIdleAsync))
            .On(AsyncTrigger.Start).GoTo(AsyncState.Working)
                .Guard(nameof(CanStartAsync))
                .Action(nameof(StartWorkAsync))
        .State(AsyncState.Working)
            .OnExitAsync(nameof(OnExitWorkingAsync))
            .On(AsyncTrigger.Finish).GoTo(AsyncState.Complete)
        .State(AsyncState.Complete)
            .On(AsyncTrigger.Reset).GoTo(AsyncState.Idle);

    private async ValueTask<bool> CanStartAsync()
    {
        await Task.Yield();
        return true;
    }

    private async Task StartWorkAsync()
    {
        await Task.Yield();
    }

    private async Task OnEnterIdleAsync()
    {
        await Task.Yield();
    }

    private async Task OnExitWorkingAsync()
    {
        await Task.Yield();
    }
}

public enum AsyncState { Idle, Working, Complete }
public enum AsyncTrigger { Start, Finish, Reset }