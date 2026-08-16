using Abstractions.Fluent;
using Abstractions.Attributes;

namespace FastFsm.Logging.Tests;

// Example state machine - Fluent version
[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class ExampleStateMachineFluent
{
    private void Configure() => FSM
        .State(OrderState.New).On(OrderTrigger.Submit).GoTo(OrderState.Submitted);
}

// Guarded state machine - Fluent version
[StateMachine(typeof(ProcessState), typeof(ProcessTrigger))]
public partial class GuardedStateMachineFluent
{
    public bool CanProcess { get; set; } = true;

    private void Configure() => FSM
        .State(ProcessState.Idle)
            .On(ProcessTrigger.Start)
                .Guard(nameof(CheckCanProcess))
                .GoTo(ProcessState.Running);

    private bool CheckCanProcess() => CanProcess;
}

// Extensible machine - Fluent version
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger), GenerateExtensibleVersion = true)]
public partial class ExtensibleMachineFluent
{
    private void Configure() => FSM
        .State(WorkflowState.Draft).On(WorkflowTrigger.Submit).GoTo(WorkflowState.Submitted);
}
