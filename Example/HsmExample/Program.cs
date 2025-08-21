using System;
using Abstractions.Attributes;

namespace HsmExample;

// Define states with hierarchical structure
public enum WorkflowState 
{ 
    Idle,
    Processing,           // Parent state
    Processing_Loading,   // Child of Processing
    Processing_Working,   // Child of Processing
    Processing_Saving,    // Child of Processing
    Complete,
    Error                 // Error state
}

public enum WorkflowTrigger 
{ 
    Start, 
    DataLoaded,
    WorkComplete,
    SaveComplete,
    UpdateProgress, 
    Cancel,
    Retry,
    Finish 
}

// Create hierarchical state machine with history support
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), EnableHierarchy = true)]
public partial class WorkflowMachine
{
    private int _progressCount = 0;
    
    // Define parent state with shallow history (remembers last substate on re-entry)
    [State(WorkflowState.Processing, 
        History = HistoryMode.Shallow,
        OnEntry = nameof(OnProcessingEntry),
        OnExit = nameof(OnProcessingExit))]
    private void ConfigureProcessing() { }

    // Define initial child state (Parent = Processing, IsInitial marks the default substate)
    [State(WorkflowState.Processing_Loading, 
        Parent = WorkflowState.Processing, 
        IsInitial = true,
        OnEntry = nameof(OnLoadingEntry))]
    private void ConfigureLoading() { }
    
    [State(WorkflowState.Processing_Working, 
        Parent = WorkflowState.Processing,
        OnEntry = nameof(OnWorkingEntry))]
    private void ConfigureWorking() { }
    
    [State(WorkflowState.Processing_Saving, 
        Parent = WorkflowState.Processing,
        OnEntry = nameof(OnSavingEntry))]
    private void ConfigureSaving() { }

    [State(WorkflowState.Idle, OnEntry = nameof(OnIdleEntry))]
    private void ConfigureIdle() { }
    
    [State(WorkflowState.Complete, OnEntry = nameof(OnCompleteEntry))]
    private void ConfigureComplete() { }
    
    [State(WorkflowState.Error, OnEntry = nameof(OnErrorEntry))]
    private void ConfigureError() { }

    // Define transitions
    [Transition(WorkflowState.Idle, WorkflowTrigger.Start, WorkflowState.Processing)]
    // When transitioning into Processing, it will automatically enter Processing_Loading (initial child)
    
    // Transitions within the Processing hierarchy
    [Transition(WorkflowState.Processing_Loading, WorkflowTrigger.DataLoaded, WorkflowState.Processing_Working)]
    [Transition(WorkflowState.Processing_Working, WorkflowTrigger.WorkComplete, WorkflowState.Processing_Saving)]
    [Transition(WorkflowState.Processing_Saving, WorkflowTrigger.SaveComplete, WorkflowState.Complete)]
    
    // Cancel from any Processing substate goes back to Idle
    [Transition(WorkflowState.Processing, WorkflowTrigger.Cancel, WorkflowState.Idle, Priority = 10)]
    
    // Error handling - can occur from any Processing substate
    [Transition(WorkflowState.Processing_Loading, WorkflowTrigger.Retry, WorkflowState.Error)]
    [Transition(WorkflowState.Processing_Working, WorkflowTrigger.Retry, WorkflowState.Error)]
    [Transition(WorkflowState.Processing_Saving, WorkflowTrigger.Retry, WorkflowState.Error)]
    
    // From Error, we can retry (goes back to Processing, which uses history to restore last substate)
    [Transition(WorkflowState.Error, WorkflowTrigger.Retry, WorkflowState.Processing)]
    
    // Internal transition in a parent state – action executes without state change
    [InternalTransition(WorkflowState.Processing, WorkflowTrigger.UpdateProgress, 
        Action = nameof(LogProgress), Priority = 5)]
    
    private void ConfigureTransitions() { }

    // Entry/Exit callbacks
    private void OnIdleEntry() => Console.WriteLine("→ Entered IDLE state");
    
    private void OnProcessingEntry() => Console.WriteLine("→ Entered PROCESSING parent state");
    private void OnProcessingExit() => Console.WriteLine("← Exiting PROCESSING parent state");
    
    private void OnLoadingEntry() => Console.WriteLine("  → Entered Loading substate");
    private void OnWorkingEntry() => Console.WriteLine("  → Entered Working substate");
    private void OnSavingEntry() => Console.WriteLine("  → Entered Saving substate");
    
    private void OnCompleteEntry() => Console.WriteLine("✓ Entered COMPLETE state - workflow finished!");
    private void OnErrorEntry() => Console.WriteLine("✗ Entered ERROR state");
    
    // Internal transition action
    private void LogProgress() 
    {
        _progressCount++;
        Console.WriteLine($"  [Progress Update #{_progressCount}] - Still in {CurrentState}");
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== FastFSM Hierarchical State Machine Example ===\n");
        
        var workflow = new WorkflowMachine(WorkflowState.Idle);
        workflow.Start(); // Initialize state machine
        
        Console.WriteLine($"Initial state: {workflow.CurrentState}\n");
        
        // Start the workflow
        Console.WriteLine("1. Starting workflow...");
        workflow.Fire(WorkflowTrigger.Start);
        Console.WriteLine($"Current state: {workflow.CurrentState}\n");
        
        // Update progress (internal transition - doesn't change state)
        Console.WriteLine("2. Updating progress (internal transition)...");
        workflow.Fire(WorkflowTrigger.UpdateProgress);
        workflow.Fire(WorkflowTrigger.UpdateProgress);
        Console.WriteLine($"Current state: {workflow.CurrentState}\n");
        
        // Progress through substates
        Console.WriteLine("3. Data loaded, moving to Working...");
        workflow.Fire(WorkflowTrigger.DataLoaded);
        Console.WriteLine($"Current state: {workflow.CurrentState}\n");
        
        // Cancel and restart (demonstrating parent state transition)
        Console.WriteLine("4. Canceling workflow...");
        workflow.Fire(WorkflowTrigger.Cancel);
        Console.WriteLine($"Current state: {workflow.CurrentState}\n");
        
        Console.WriteLine("5. Restarting workflow...");
        workflow.Fire(WorkflowTrigger.Start);
        Console.WriteLine($"Current state: {workflow.CurrentState}");
        Console.WriteLine("   (Should be Loading again as it's the initial substate)\n");
        
        // Progress to Working, then simulate error
        Console.WriteLine("6. Progressing to Working state...");
        workflow.Fire(WorkflowTrigger.DataLoaded);
        workflow.Fire(WorkflowTrigger.UpdateProgress);
        Console.WriteLine($"Current state: {workflow.CurrentState}\n");
        
        Console.WriteLine("7. Simulating error...");
        workflow.Fire(WorkflowTrigger.Retry); // This triggers error from Working
        Console.WriteLine($"Current state: {workflow.CurrentState}\n");
        
        // Retry - should restore to Working due to shallow history
        Console.WriteLine("8. Retrying (should restore to Working due to history)...");
        workflow.Fire(WorkflowTrigger.Retry);
        Console.WriteLine($"Current state: {workflow.CurrentState}");
        Console.WriteLine("   (History restored the last active substate!)\n");
        
        // Complete the workflow
        Console.WriteLine("9. Completing the workflow...");
        workflow.Fire(WorkflowTrigger.WorkComplete); // Working -> Saving
        Console.WriteLine($"Current state: {workflow.CurrentState}");
        workflow.Fire(WorkflowTrigger.SaveComplete); // Saving -> Complete
        Console.WriteLine($"Final state: {workflow.CurrentState}\n");
        
        // Show permitted triggers
        Console.WriteLine("10. Available triggers in Complete state:");
        var triggers = workflow.GetPermittedTriggers();
        if (triggers.Count == 0)
            Console.WriteLine("   No triggers available (terminal state)");
        else
            triggers.ToList().ForEach(t => Console.WriteLine($"   - {t}"));
        
        Console.WriteLine("\n=== Demo Complete ===");
    }
}