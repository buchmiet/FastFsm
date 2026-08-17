using Abstractions.Attributes;
namespace Tests.Machines.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class SimpleParentChildMachine
{
    // Parent state with children
    [State(HsmState.Working,
        OnEntry = nameof(OnWorkingEntry),
        OnExit = nameof(OnWorkingExit))]
    private void ConfigureWorking() { }

    // Child states with proper Parent reference
    [State(HsmState.Working_Initializing,
        Parent = HsmState.Working,
        IsInitial = true,
        OnEntry = nameof(OnInitializingEntry),
        OnExit = nameof(OnInitializingExit))]
    private void ConfigureInitializing() { }

    [State(HsmState.Working_Processing,
        Parent = HsmState.Working,
        OnEntry = nameof(OnProcessingEntry))]
    private void ConfigureProcessing() { }

    [State(HsmState.Working_Validating,
        Parent = HsmState.Working)]
    private void ConfigureValidating() { }

    // Valid transitions
    [Transition(HsmState.Idle, HsmTrigger.Start, HsmState.Working)]
    [Transition(HsmState.Working_Initializing, HsmTrigger.Process, HsmState.Working_Processing)]
    [Transition(HsmState.Working_Processing, HsmTrigger.Validate, HsmState.Working_Validating)]
    [Transition(HsmState.Working, HsmTrigger.Complete, HsmState.Completed)]
    private void ConfigureTransitions() { }

    // Callback methods
    private void OnWorkingEntry() { }
    private void OnWorkingExit() { }
    private void OnInitializingEntry() { }
    private void OnInitializingExit() { }
    private void OnProcessingEntry() { }
}
