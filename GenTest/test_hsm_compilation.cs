using Abstractions.Attributes;
using System;

// Test HSM from FastFsm.Tests
public enum HsmState
{
    // Root states
    Idle,
    Working,
    Completed,
    Error,
    
    // Working substates (2nd level)
    Working_Initializing,
    Working_Processing,
    Working_Validating,
    Working_Cleanup,
    
    // Working_Processing substates (3rd level)
    Working_Processing_Reading,
    Working_Processing_Computing,
    Working_Processing_Writing,
    
    // Working_Processing_Computing substates (4th level - deep hierarchy)
    Working_Processing_Computing_Loading,
    Working_Processing_Computing_Calculating,
    Working_Processing_Computing_Storing
}

public enum HsmTrigger
{
    Start,
    Process,
    Complete,
    Validate,
    Execute,
    Pause,
    Resume,
    Reset,
    Initialize,
    Continue,
    Abort,
    Finish,
    MoveNext,
    MovePrevious,
    Return,
    Navigate,
    Run,
    Stop,
    Update
}

// Deep Hierarchy Machine - 4 Levels (VALID)
[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class DeepHierarchyMachine
{
    // Level 1
    [State(HsmState.Working)]
    private void ConfigureLevel1() { }
    
    // Level 2
    [State(HsmState.Working_Processing, 
        Parent = HsmState.Working,
        IsInitial = true)]
    private void ConfigureLevel2() { }
    
    // Level 3
    [State(HsmState.Working_Processing_Computing, 
        Parent = HsmState.Working_Processing,
        IsInitial = true)]
    private void ConfigureLevel3() { }
    
    // Level 4
    [State(HsmState.Working_Processing_Computing_Loading, 
        Parent = HsmState.Working_Processing_Computing, 
        IsInitial = true, 
        OnEntry = nameof(OnLoadingEntry))]
    private void ConfigureLoading() { }
    
    [State(HsmState.Working_Processing_Computing_Calculating, 
        Parent = HsmState.Working_Processing_Computing, 
        OnEntry = nameof(OnCalculatingEntry), 
        OnExit = nameof(OnCalculatingExit))]
    private void ConfigureCalculating() { }
    
    [State(HsmState.Working_Processing_Computing_Storing, 
        Parent = HsmState.Working_Processing_Computing)]
    private void ConfigureStoring() { }

    // Cross-level transitions
    [Transition(HsmState.Working_Processing_Computing_Loading, HsmTrigger.Process, HsmState.Working_Processing_Computing_Calculating)]
    [Transition(HsmState.Working_Processing_Computing_Calculating, HsmTrigger.Complete, HsmState.Working_Processing_Computing_Storing)]
    [Transition(HsmState.Working_Processing_Computing_Storing, HsmTrigger.Finish, HsmState.Completed)]
    [Transition(HsmState.Working, HsmTrigger.Abort, HsmState.Error)]
    private void ConfigureDeepTransitions() { }
    
    // Callback methods
    private void OnLoadingEntry() { }
    private void OnCalculatingEntry() { }
    private void OnCalculatingExit() { }
}