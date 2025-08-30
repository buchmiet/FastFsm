using Abstractions.Attributes;
using System.Threading.Tasks;
using System.Threading;

// Async FSM with cancellation token support
[StateMachine(typeof(TaskState), typeof(TaskTrigger))]
public partial class TaskMachine 
{
    // Async state callbacks
    [State(TaskState.Ready, OnEntry = "OnReadyAsync")]
    [State(TaskState.Running, OnEntry = "OnRunningAsync", OnExit = "OnStopRunningAsync")]
    [State(TaskState.Completed)]
    [State(TaskState.Failed, OnEntry = "OnFailedAsync")]
    private void ConfigureStates() { }
    
    // Transitions with async guards and actions
    [Transition(TaskState.Ready, TaskTrigger.Start, TaskState.Running, 
        Guard = "CanStartAsync", Action = "StartTaskAsync")]
    [Transition(TaskState.Running, TaskTrigger.Complete, TaskState.Completed)]
    [Transition(TaskState.Running, TaskTrigger.Fail, TaskState.Failed)]
    [Transition(TaskState.Failed, TaskTrigger.Retry, TaskState.Ready)]
    private void ConfigureTransitions() { }
    
    // Async callbacks with CancellationToken
    private async Task OnReadyAsync(CancellationToken ct) 
    {
        await Task.Delay(100, ct);
    }
    
    private async Task OnRunningAsync(CancellationToken ct) 
    {
        await Task.Delay(500, ct);
    }
    
    private async Task OnStopRunningAsync() 
    {
        await Task.Delay(50);
    }
    
    private async Task OnFailedAsync(CancellationToken ct) 
    {
        await Task.Delay(200, ct);
    }
    
    // Async guard
    private async Task<bool> CanStartAsync(CancellationToken ct) 
    {
        await Task.Delay(10, ct);
        return true;
    }
    
    // Async action
    private async Task StartTaskAsync(CancellationToken ct) 
    {
        await Task.Delay(100, ct);
    }
}

public enum TaskState 
{ 
    Ready,
    Running,
    Completed,
    Failed
}

public enum TaskTrigger 
{ 
    Start,
    Complete,
    Fail,
    Retry
}