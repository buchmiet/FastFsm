using Abstractions.Attributes;
using Generator.Model;

namespace TestHsm;

public enum ProcessState 
{ 
    Pending, 
    Work,           // Parent
    Work_Idle,      // Child of Work
    Work_Active,    // Child of Work
    Done
}

public enum ProcessTrigger 
{ 
    Start, 
    Activate,
    Finish
}

[StateMachine(typeof(ProcessState), typeof(ProcessTrigger), EnableHierarchy = true)]
public partial class TestHsmMachine
{
    // Test Parent i IsInitial
    [State(ProcessState.Work_Idle, 
        Parent = ProcessState.Work, 
        IsInitial = true)]
    private void ConfigureIdleState() { }
    
    // Test History
    [State(ProcessState.Work, 
        History = HistoryMode.Shallow)]
    private void ConfigureWorkState() { }
    
    // Test Priority w Transition
    [Transition(ProcessState.Work_Idle, ProcessTrigger.Activate, ProcessState.Work_Active,
        Priority = 100)]
    private void ConfigureActivation() { }
    
    // Test Priority w InternalTransition
    [InternalTransition(ProcessState.Work_Active, ProcessTrigger.Start, nameof(DoWork),
        Priority = 50)]
    private void ConfigureInternalWork() { }
    
    private void DoWork() { }
}