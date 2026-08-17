namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), GenerateExtensibleVersion = true)]
public partial class EmptyExtensibleMachine
{
    [Transition(WorkflowState.Draft, WorkflowTrigger.Submit, WorkflowState.Submitted)]
    private void Configure() { }
}