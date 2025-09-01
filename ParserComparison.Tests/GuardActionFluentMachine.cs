using Abstractions.Attributes;
using Abstractions.Fluent;

namespace ParserComparison.Tests;

// This machine uses Fluent API to define the same state machine as GuardActionStateMachine
[StateMachine(typeof(GuardState), typeof(GuardTrigger))]
public partial class GuardActionFluentMachine
{
    private int _quota = 5;
    
    private static void Configure() => FSM
        .State(GuardState.Idle)
            .OnEntry(nameof(OnIdleEntry))
            .On(GuardTrigger.Start).GoTo(GuardState.Running)
                .Guard(nameof(HasQuota)).Action(nameof(OnStart))
        .State(GuardState.Running)
            .On(GuardTrigger.Stop).GoTo(GuardState.Stopped)
                .Action(nameof(OnStop))
        .State(GuardState.Stopped)
            .OnExit(nameof(OnStoppedExit));
    
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