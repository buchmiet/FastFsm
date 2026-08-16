using Abstractions.Attributes;

namespace ParserComparison.Tests;

[StateMachine(typeof(GuardState), typeof(GuardTrigger))]
public partial class GuardActionStateMachine
{
    private int _quota = 5;
    
    [State(GuardState.Idle, OnEntry = nameof(OnIdleEntry))]
    private void Idle()
    {
    }
    
    [State(GuardState.Running)]
    private void Running()
    {
    }
    
    [State(GuardState.Stopped, OnExit = nameof(OnStoppedExit))]
    private void Stopped()
    {
    }
    
    [Transition(GuardState.Idle, GuardTrigger.Start, GuardState.Running, 
        Guard = nameof(HasQuota), Action = nameof(OnStart))]
    private void StartTransition()
    {
    }
    
    [Transition(GuardState.Running, GuardTrigger.Stop, GuardState.Stopped, 
        Action = nameof(OnStop))]
    private void StopTransition()
    {
    }
    
    // Guards
    private bool HasQuota() => _quota > 0;
    
    // Actions
    private void OnStart() 
    { 
        _quota--;
        Console.WriteLine($"Started. Quota remaining: {_quota}");
    }
    
    private void OnStop() 
    { 
        Console.WriteLine("Stopping...");
    }
    
    // Entry/Exit actions
    private void OnIdleEntry()
    {
        Console.WriteLine("Entered Idle state");
    }
    
    private void OnStoppedExit()
    {
        Console.WriteLine("Exiting Stopped state");
    }
}

public enum GuardState
{
    Idle,
    Running,
    Stopped
}

public enum GuardTrigger
{
    Start,
    Stop
}