using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    // Fluent version (variant B)
    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class DeepHierarchyMachine_Fluent
    {
        public static void Configure()
        {
            // Level 1 (parent)
            FSM.State(HsmState.Working)
               .Initial(HsmState.Working_Processing)
               .On(HsmTrigger.Abort).GoTo(HsmState.Error);

            // Level 2 (child of Working)
            FSM.State(HsmState.Working_Processing)
               .ChildOf(HsmState.Working)
               .Initial(HsmState.Working_Processing_Computing);

            // Level 3 (child of Working_Processing)
            FSM.State(HsmState.Working_Processing_Computing)
               .ChildOf(HsmState.Working_Processing)
               .Initial(HsmState.Working_Processing_Computing_Loading);

            // Level 4 leaves + cross-level transitions
            FSM.State(HsmState.Working_Processing_Computing_Loading)
               .ChildOf(HsmState.Working_Processing_Computing)
               .On(HsmTrigger.Process).GoTo(HsmState.Working_Processing_Computing_Calculating);

            FSM.State(HsmState.Working_Processing_Computing_Calculating)
               .ChildOf(HsmState.Working_Processing_Computing)
               .On(HsmTrigger.Complete).GoTo(HsmState.Working_Processing_Computing_Storing);

            FSM.State(HsmState.Working_Processing_Computing_Storing)
               .ChildOf(HsmState.Working_Processing_Computing)
               .On(HsmTrigger.Finish).GoTo(HsmState.Completed);

            FSM.State(HsmState.Completed);
            FSM.State(HsmState.Error);
        }
    }
}