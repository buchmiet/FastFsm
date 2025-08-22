using Abstractions.Attributes;

namespace FsmTest;

// HSM example from README
public enum WorkflowState 
{ 
    Idle,
    Processing,           // Parent state
    Processing_Loading,   // Child of Processing
    Processing_Working,   // Child of Processing
    Processing_Saving,    // Child of Processing
    Complete
}

public enum WorkflowTrigger { Start, UpdateProgress, Finish, Next }

[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), EnableHierarchy = true)]
public partial class WorkflowMachine
{
    // Define parent state with shallow history
    [State(WorkflowState.Processing, History = HistoryMode.Shallow)]
    private void ConfigureProcessing() { }

    // Define initial child state
    [State(WorkflowState.Processing_Loading, Parent = WorkflowState.Processing, IsInitial = true)]
    private void ConfigureLoading() { }
    
    [State(WorkflowState.Processing_Working, Parent = WorkflowState.Processing)]
    private void ConfigureWorking() { }
    
    [State(WorkflowState.Processing_Saving, Parent = WorkflowState.Processing)]
    private void ConfigureSaving() { }

    // Define transitions
    [Transition(WorkflowState.Idle, WorkflowTrigger.Start, WorkflowState.Processing)]
    [Transition(WorkflowState.Processing_Loading, WorkflowTrigger.Next, WorkflowState.Processing_Working)]
    [Transition(WorkflowState.Processing_Working, WorkflowTrigger.Next, WorkflowState.Processing_Saving)]
    [Transition(WorkflowState.Processing_Saving, WorkflowTrigger.Finish, WorkflowState.Complete)]
    private void ConfigureTransitions() { }

    // Internal transition in parent state
    [InternalTransition(WorkflowState.Processing, WorkflowTrigger.UpdateProgress, Action = nameof(LogProgress))]
    private void ConfigureInternalTransitions() { }

    private void LogProgress() => Console.WriteLine("Progress updated.");
}