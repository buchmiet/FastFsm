using Xunit;
using Dsl;
using System.Collections.Generic;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public partial class DeepHistoryTestsFluent
    {
        [Fact]
        public void DeepHistory_Restores_LeafPath_After_ReenteringFluent()
        {
            var m = new DeepHistoryMachineFluent(S.Out);
            m.Start();

            // Enter composite → auto path: Work → S1 (initial) → Loading (initial)
            m.Fire(T.EnterWork);
            Assert.Equal(S.Work_S1_Loading, m.CurrentState);

            // Move to deeper sibling leaf
            m.Fire(T.Next);
            Assert.Equal(S.Work_S1_Calc, m.CurrentState);

            // Exit composite to outside
            m.Fire(T.Abort);
            Assert.Equal(S.Out, m.CurrentState);

            // Re-enter → deep history returns to the last leaf (Calc)
            m.Fire(T.EnterWork);
            Assert.Equal(S.Work_S1_Calc, m.CurrentState);
        }

        public enum S { Out, Work, Work_S1, Work_S1_Loading, Work_S1_Calc }
        public enum T { EnterWork, Next, Abort }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class DeepHistoryMachineFluent
        {
            public static void Configure()
            {
                // Define root state with deep history
                FSM.State(S.Work)
                   .Initial(S.Work_S1)
                   .HistoryDeep()
                   .On(T.Abort).GoTo(S.Out);

                // Define level 2 state
                FSM.State(S.Work_S1)
                   .ChildOf(S.Work)
                   .Initial(S.Work_S1_Loading);

                // Define level 3 states
                FSM.State(S.Work_S1_Loading)
                   .ChildOf(S.Work_S1)
                   .On(T.Next).GoTo(S.Work_S1_Calc);

                FSM.State(S.Work_S1_Calc)
                   .ChildOf(S.Work_S1);

                // Define outside state
                FSM.State(S.Out)
                   .On(T.EnterWork).GoTo(S.Work);
            }
        }
    }
}