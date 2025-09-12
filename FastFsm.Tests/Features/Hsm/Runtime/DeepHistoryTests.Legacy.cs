using Xunit;
using Abstractions.Attributes;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public partial class DeepHistoryTestsLegacy
    {
        [Fact]
        public void DeepHistory_Restores_LeafPath_After_ReenteringLegacy()
        {
            var m = new DeepHistoryMachineLegacy(DeepHistoryTestsFluent.S.Out);
            m.Start();

            // Enter composite → auto path: Work → S1 (initial) → Loading (initial)
            m.Fire(DeepHistoryTestsFluent.T.EnterWork);
            Assert.Equal(DeepHistoryTestsFluent.S.Work_S1_Loading, m.CurrentState);

            // Move to deeper sibling leaf
            m.Fire(DeepHistoryTestsFluent.T.Next);
            Assert.Equal(DeepHistoryTestsFluent.S.Work_S1_Calc, m.CurrentState);

            // Exit composite to outside
            m.Fire(DeepHistoryTestsFluent.T.Abort);
            Assert.Equal(DeepHistoryTestsFluent.S.Out, m.CurrentState);

            // Re-enter → deep history returns to the last leaf (Calc)
            m.Fire(DeepHistoryTestsFluent.T.EnterWork);
            Assert.Equal(DeepHistoryTestsFluent.S.Work_S1_Calc, m.CurrentState);
        }

        [StateMachine(typeof(DeepHistoryTestsFluent.S), typeof(DeepHistoryTestsFluent.T), EnableHierarchy = true)]
        public partial class DeepHistoryMachineLegacy
        {
            // Define root state with deep history
            [State(DeepHistoryTestsFluent.S.Work, History = HistoryMode.Deep)]
            private void ConfigureWork() { }

            // Define level 2 state
            [State(DeepHistoryTestsFluent.S.Work_S1, Parent = DeepHistoryTestsFluent.S.Work, IsInitial = true)]
            private void ConfigureWorkS1() { }

            // Define level 3 states
            [State(DeepHistoryTestsFluent.S.Work_S1_Loading, Parent = DeepHistoryTestsFluent.S.Work_S1, IsInitial = true)]
            private void ConfigureWorkS1Loading() { }

            [State(DeepHistoryTestsFluent.S.Work_S1_Calc, Parent = DeepHistoryTestsFluent.S.Work_S1)]
            private void ConfigureWorkS1Calc() { }

            // Define outside state
            [State(DeepHistoryTestsFluent.S.Out)]
            private void ConfigureOut() { }

            // Transitions
            [Transition(DeepHistoryTestsFluent.S.Out, DeepHistoryTestsFluent.T.EnterWork, DeepHistoryTestsFluent.S.Work)]
            private void ConfigureOutToWork() { }

            [Transition(DeepHistoryTestsFluent.S.Work_S1_Loading, DeepHistoryTestsFluent.T.Next, DeepHistoryTestsFluent.S.Work_S1_Calc)]
            private void ConfigureLoadingToCalc() { }

            [Transition(DeepHistoryTestsFluent.S.Work, DeepHistoryTestsFluent.T.Abort, DeepHistoryTestsFluent.S.Out)]
            private void ConfigureWorkToOut() { }
        }
    }
}