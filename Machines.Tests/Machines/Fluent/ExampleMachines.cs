using Abstractions.Fluent;

namespace Machines.Tests.Machines.Fluent;

// Example state machine - Fluent version

// Guarded state machine - Fluent version

// Extensible machine - Fluent version
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensibleMachine
{
    private static void Configure() => FSM
        .State(WorkflowState.Draft).On(WorkflowTrigger.Submit).GoTo(WorkflowState.Submitted);
}