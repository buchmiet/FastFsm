using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    // Top-level class to compare with nested
    [StateMachine(typeof(DebugState), typeof(DebugTrigger), EnableHierarchy = true)]
    public partial class DebugFluentParser_TopLevel
    {
        public enum DebugState { A, B, C }
        public enum DebugTrigger { Go }

        private static void Configure()
        {
            FSM.State(DebugState.A)
                .Initial(DebugState.B);
                
            FSM.State(DebugState.B)
                .ChildOf(DebugState.A);
            
            FSM.At(DebugState.C)
                .On(DebugTrigger.Go)
                .GoTo(DebugState.A);
        }
    }
    
    // Container for nested test
    public partial class DebugContainer
    {
        // Nested class  
        [StateMachine(typeof(DebugState), typeof(DebugTrigger), EnableHierarchy = true)]
        public partial class DebugFluentParser_Nested
        {
            public enum DebugState { A, B, C }
            public enum DebugTrigger { Go }

            private static void Configure()
            {
                FSM.State(DebugState.A)
                    .Initial(DebugState.B);
                    
                FSM.State(DebugState.B)
                    .ChildOf(DebugState.A);
                
                FSM.At(DebugState.C)
                    .On(DebugTrigger.Go)
                    .GoTo(DebugState.A);
            }
        }
    }
}