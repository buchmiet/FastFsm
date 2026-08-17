using Abstractions.Fluent;

namespace Tests.Machines.Machines.Fluent;

// Example state machine - Fluent version

// Guarded state machine - Fluent version

// Extensible machine - Fluent version
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensibleMachine
{
    private void Configure() => FSM
        .State(WorkflowState.Draft).On(WorkflowTrigger.Submit).GoTo(WorkflowState.Submitted);
}