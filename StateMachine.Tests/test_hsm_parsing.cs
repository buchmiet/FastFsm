using Abstractions.Attributes;
using System;

namespace StateMachine.Tests.HierarchicalTests;

public enum ProcessState 
{ 
    Pending, 
    Work,           // Parent
    Work_Idle,      // Child of Work
    Work_Active,    // Child of Work  
    Work_Paused,    // Child of Work
    Done,
    Error
}

public enum ProcessTrigger 
{ 
    Start, 
    Activate,
    Pause,
    Resume,
    Finish,
    Abort
}

[StateMachine(typeof(ProcessState), typeof(ProcessTrigger), EnableHierarchy = true)]
public partial class TestHsmMachine
{
    // Parent state z historią
    [State(ProcessState.Work, 
        History = HistoryMode.Shallow,
        OnEntry = nameof(EnterWork),
        OnExit = nameof(ExitWork))]
    private void ConfigureWorkState() { }
    
    // Initial child state
    [State(ProcessState.Work_Idle, 
        Parent = ProcessState.Work, 
        IsInitial = true)]
    private void ConfigureIdleState() { }
    
    // Inne child states
    [State(ProcessState.Work_Active, 
        Parent = ProcessState.Work)]
    private void ConfigureActiveState() { }
    
    [State(ProcessState.Work_Paused, 
        Parent = ProcessState.Work)]
    private void ConfigurePausedState() { }
    
    // Transitions między child states
    [Transition(ProcessState.Work_Idle, ProcessTrigger.Activate, ProcessState.Work_Active,
        Priority = 100)]
    private void ConfigureActivation() { }
    
    [Transition(ProcessState.Work_Active, ProcessTrigger.Pause, ProcessState.Work_Paused)]
    private void ConfigurePause() { }
    
    [Transition(ProcessState.Work_Paused, ProcessTrigger.Resume, ProcessState.Work_Active)]
    private void ConfigureResume() { }
    
    // Transition z hierarchii
    [Transition(ProcessState.Work_Active, ProcessTrigger.Finish, ProcessState.Done,
        Priority = 200)]
    private void ConfigureFinish() { }
    
    // Transition z parent state (niższy priorytet)
    [Transition(ProcessState.Work, ProcessTrigger.Abort, ProcessState.Error,
        Priority = 50)]
    private void ConfigureAbort() { }
    
    // Entry z zewnątrz do hierarchii
    [Transition(ProcessState.Pending, ProcessTrigger.Start, ProcessState.Work)]
    private void ConfigureStart() { }
    
    // Internal transition
    [InternalTransition(ProcessState.Work_Active, ProcessTrigger.Start, "DoWork",
        Priority = 50)]
    private void ConfigureInternalWork() { }
    
    // Callback methods
    private void EnterWork() { }
    private void ExitWork() { }
    private void DoWork() { }
}