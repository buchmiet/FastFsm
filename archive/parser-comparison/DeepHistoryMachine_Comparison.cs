using Abstractions.Attributes;
using Dsl;
using System.Collections.Generic;

namespace ParserComparison.Tests
{
    // Test enums for DeepHistory machine
    public enum DeepHistoryState 
    { 
        Out, 
        Work, 
        Work_S1, 
        Work_S1_Loading, 
        Work_S1_Calc 
    }
    
    public enum DeepHistoryTrigger 
    { 
        EnterWork, 
        Next, 
        Abort 
    }

    // Attribute-based version (legacy)
    [StateMachine(typeof(DeepHistoryState), typeof(DeepHistoryTrigger), EnableHierarchy = true)]
    public partial class DeepHistoryMachine_Attributes_V2
    {
        public List<string> Log { get; } = new();

        [State(DeepHistoryState.Work, History = HistoryMode.Deep)] 
        private void Work() { }
        
        [State(DeepHistoryState.Work_S1, Parent = DeepHistoryState.Work, IsInitial = true)] 
        private void S1() { }
        
        [State(DeepHistoryState.Work_S1_Loading, Parent = DeepHistoryState.Work_S1, IsInitial = true)] 
        private void Loading() { }
        
        [State(DeepHistoryState.Work_S1_Calc, Parent = DeepHistoryState.Work_S1)] 
        private void Calc() { }

        [Transition(DeepHistoryState.Out, DeepHistoryTrigger.EnterWork, DeepHistoryState.Work)]
        [Transition(DeepHistoryState.Work_S1_Loading, DeepHistoryTrigger.Next, DeepHistoryState.Work_S1_Calc)]
        [Transition(DeepHistoryState.Work, DeepHistoryTrigger.Abort, DeepHistoryState.Out)]
        private void Configure() { }
    }

    // Fluent API version
    [StateMachine(typeof(DeepHistoryState), typeof(DeepHistoryTrigger), EnableHierarchy = true)]
    public partial class DeepHistoryMachine_Fluent_V2
    {
        public List<string> Log { get; } = new();

        public static void Configure()
        {
            // Define root state with deep history
            FSM.State(DeepHistoryState.Work)
               .Initial(DeepHistoryState.Work_S1)
               .HistoryDeep()
               .On(DeepHistoryTrigger.Abort).GoTo(DeepHistoryState.Out);

            // Define level 2 state
            FSM.State(DeepHistoryState.Work_S1)
               .ChildOf(DeepHistoryState.Work)
               .Initial(DeepHistoryState.Work_S1_Loading);

            // Define level 3 states
            FSM.State(DeepHistoryState.Work_S1_Loading)
               .ChildOf(DeepHistoryState.Work_S1)
               .On(DeepHistoryTrigger.Next).GoTo(DeepHistoryState.Work_S1_Calc);

            FSM.State(DeepHistoryState.Work_S1_Calc)
               .ChildOf(DeepHistoryState.Work_S1);

            // Define outside state
            FSM.State(DeepHistoryState.Out)
               .On(DeepHistoryTrigger.EnterWork).GoTo(DeepHistoryState.Work);
        }
    }
}