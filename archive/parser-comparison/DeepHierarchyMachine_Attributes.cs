using Abstractions.Attributes;

namespace ParserComparison.Tests
{
    // HSM Test enums
    public enum HsmState
    {
        // Root states
        Working,
        Completed,
        Error,

        // Working_Processing substates (3rd level)
        Working_Processing,
        Working_Processing_Computing,

        // Level 4
        Working_Processing_Computing_Loading,
        Working_Processing_Computing_Calculating,
        Working_Processing_Computing_Storing,
    }

    public enum HsmTrigger
    {
        Process,
        Complete,
        Finish,
        Abort
    }

    // Attribute-based version
    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class DeepHierarchyMachine_Attributes
    {
        // Level 1 (parent)
        [State(HsmState.Working)]
        [Transition(HsmState.Working, HsmTrigger.Abort, HsmState.Error)]
        void ConfigureWorking() { }

        // Level 2 (child of Working) - marked as initial via IsInitial
        [State(HsmState.Working_Processing, Parent = HsmState.Working, IsInitial = true)]
        void ConfigureWorking_Processing() { }

        // Level 3 (child of Working_Processing) - marked as initial via IsInitial
        [State(HsmState.Working_Processing_Computing, Parent = HsmState.Working_Processing, IsInitial = true)]
        void ConfigureWorking_Processing_Computing() { }

        // Level 4 leaves + cross-level transitions
        [State(HsmState.Working_Processing_Computing_Loading, Parent = HsmState.Working_Processing_Computing, IsInitial = true)]
        [Transition(HsmState.Working_Processing_Computing_Loading, HsmTrigger.Process, HsmState.Working_Processing_Computing_Calculating)]
        void ConfigureWorking_Processing_Computing_Loading() { }

        [State(HsmState.Working_Processing_Computing_Calculating, Parent = HsmState.Working_Processing_Computing)]
        [Transition(HsmState.Working_Processing_Computing_Calculating, HsmTrigger.Complete, HsmState.Working_Processing_Computing_Storing)]
        void ConfigureWorking_Processing_Computing_Calculating() { }

        [State(HsmState.Working_Processing_Computing_Storing, Parent = HsmState.Working_Processing_Computing)]
        [Transition(HsmState.Working_Processing_Computing_Storing, HsmTrigger.Finish, HsmState.Completed)]
        void ConfigureWorking_Processing_Computing_Storing() { }

        // Top-level states
        [State(HsmState.Completed)]
        void ConfigureCompleted() { }

        [State(HsmState.Error)]
        void ConfigureError() { }
    }
}