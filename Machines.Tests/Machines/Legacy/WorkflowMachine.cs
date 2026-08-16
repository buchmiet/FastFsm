using Machines.Tests.Payloads;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;


[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger))]
[PayloadType(typeof(WorkflowPayload))]
public partial class WorkflowMachine
{
    public int Priority { get; private set; }
    public string ApprovedBy { get; private set; } = null!;
    public string Result { get; private set; } = null!;

    [State(WorkflowState.Initialized, OnEntry = nameof(OnInitialized))]
    [State(WorkflowState.Approved, OnEntry = nameof(OnApproved))]
    [State(WorkflowState.Completed, OnEntry = nameof(OnCompleted))]
    private void ConfigureStates() { }

    [Transition(WorkflowState.Created, WorkflowTrigger.Initialize, WorkflowState.Initialized)]
    [Transition(WorkflowState.Initialized, WorkflowTrigger.Submit, WorkflowState.Submitted)]
    [Transition(WorkflowState.Submitted, WorkflowTrigger.Approve, WorkflowState.Approved)]
    [Transition(WorkflowState.Approved, WorkflowTrigger.Complete, WorkflowState.Completed)]
    private void Configure() { }

    private void OnInitialized(WorkflowPayload payload)
    {
        Priority = payload.Priority;
    }

    private void OnApproved(WorkflowPayload payload)
    {
        ApprovedBy = payload.ApprovedBy;
    }

    private void OnCompleted(WorkflowPayload payload)
    {
        Result = payload.Result;
    }
}
