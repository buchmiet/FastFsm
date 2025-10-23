using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class SimpleParentChildMachine
{
    // Parent state with children
    [State(HsmState.Working,
        OnEntry = (OnWorkingEntry),
        OnExit = (OnWorkingExit))]
    private void ConfigureWorking() { }

    // Child states with proper Parent reference
    [State(HsmState.Working_Initializing,
        Parent = HsmState.Working,
        IsInitial = true,
        OnEntry = (OnInitializingEntry),
        OnExit = (OnInitializingExit))]
    private void ConfigureInitializing() { }

    [State(HsmState.Working_Processing,
        Parent = HsmState.Working,
        OnEntry = (OnProcessingEntry))]
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
