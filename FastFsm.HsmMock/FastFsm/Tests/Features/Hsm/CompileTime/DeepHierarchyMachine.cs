using Abstractions.Attributes;
using Dsl;

namespace FastFsm.Tests.Features.Hsm.CompileTime
{
    [StateMachine(typeof(HsmState), typeof(HsmTrigger), EnableHierarchy = true)]
    public partial class DeepHierarchyMachine
    {
        public static void Configure()
        {
            FSM.State(HsmState.Working)
               .Initial(HsmState.Working_Processing)
               .On(HsmTrigger.Abort).GoTo(HsmState.Error);

            FSM.State(HsmState.Working_Processing)
               .ChildOf(HsmState.Working)
               .Initial(HsmState.Working_Processing_Computing);

            FSM.State(HsmState.Working_Processing_Computing)
               .ChildOf(HsmState.Working_Processing)
               .Initial(HsmState.Working_Processing_Computing_Loading);

            FSM.State(HsmState.Working_Processing_Computing_Loading)
               .ChildOf(HsmState.Working_Processing_Computing)
               .On(HsmTrigger.Process).GoTo(HsmState.Working_Processing_Computing_Calculating);

            FSM.State(HsmState.Working_Processing_Computing_Calculating)
               .ChildOf(HsmState.Working_Processing_Computing)
               .On(HsmTrigger.Complete).GoTo(HsmState.Working_Processing_Computing_Storing);

            FSM.State(HsmState.Working_Processing_Computing_Storing)
               .ChildOf(HsmState.Working_Processing_Computing)
               .On(HsmTrigger.Finish).GoTo(HsmState.Completed);
        }
    }
}