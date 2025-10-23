using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class InitialStateMachineHsm
{
    // Parent with explicit initial child
    [State(HsmState.Working)]
    private void ConfigureWorkingParent() { }

    // Multiple children, one marked as initial
    [State(HsmState.Working_Initializing,
        Parent = HsmState.Working,
        IsInitial = true)]
    private void ConfigureInitial() { }

    [State(HsmState.Working_Processing,
        Parent = HsmState.Working,
        IsInitial = false)]  // Explicitly not initial
    private void ConfigureNonInitial() { }

    [State(HsmState.Working_Validating,
        Parent = HsmState.Working)]  // Default (not initial)
    private void ConfigureDefault() { }

    [State(HsmState.Working_Cleanup,
        Parent = HsmState.Working)]
    private void ConfigureCleanup() { }
}
