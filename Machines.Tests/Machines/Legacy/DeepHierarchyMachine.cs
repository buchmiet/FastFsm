using Abstractions.Attributes;
namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
public partial class DeepHierarchyMachine
{
    // Level 1
    [State(HsmState.Working)]
    private void ConfigureLevel1() { }

    // Level 2
    [State(HsmState.Working_Processing,
        Parent = HsmState.Working,
        IsInitial = true)]
    private void ConfigureLevel2() { }

    // Level 3
    [State(HsmState.Working_Processing_Computing,
        Parent = HsmState.Working_Processing,
        IsInitial = true)]
    private void ConfigureLevel3() { }

    // Level 4
    [State(HsmState.Working_Processing_Computing_Loading,
        Parent = HsmState.Working_Processing_Computing,
        IsInitial = true,
        OnEntry = (OnLoadingEntry))]
    private void ConfigureLoading() { }

    [State(HsmState.Working_Processing_Computing_Calculating,
        Parent = HsmState.Working_Processing_Computing,
        OnEntry = (OnCalculatingEntry),
        OnExit = (OnCalculatingExit))]
    private void ConfigureCalculating() { }

    [State(HsmState.Working_Processing_Computing_Storing,
        Parent = HsmState.Working_Processing_Computing)]
    private void ConfigureStoring() { }

    // Cross-level transitions
    [Transition(HsmState.Working_Processing_Computing_Loading, HsmTrigger.Process, HsmState.Working_Processing_Computing_Calculating)]
    [Transition(HsmState.Working_Processing_Computing_Calculating, HsmTrigger.Complete, HsmState.Working_Processing_Computing_Storing)]
    [Transition(HsmState.Working_Processing_Computing_Storing, HsmTrigger.Finish, HsmState.Completed)]
    [Transition(HsmState.Working, HsmTrigger.Abort, HsmState.Error)]
    private void ConfigureDeepTransitions() { }

    // Callback methods
    private void OnLoadingEntry() { }
    private void OnCalculatingEntry() { }
    private void OnCalculatingExit() { }
}
