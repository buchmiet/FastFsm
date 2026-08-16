using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    // Test enums for InitialChild machine
    public enum InitialChildState 
    { 
        Outside, 
        Parent, 
        Parent_A, 
        Parent_B 
    }
    
    public enum InitialChildTrigger 
    { 
        EnterParent, 
        Switch, 
        LeaveParent 
    }

    // Attribute-based version (legacy)
    [StateMachine(typeof(InitialChildState), typeof(InitialChildTrigger), EnableHierarchy = true)]
    public partial class InitialChildMachine_Attributes
    {
        [State(InitialChildState.Parent)] 
        private void Parent() { }
        
        [State(InitialChildState.Parent_A, Parent = InitialChildState.Parent, IsInitial = true)] 
        private void ChildA() { }
        
        [State(InitialChildState.Parent_B, Parent = InitialChildState.Parent)] 
        private void ChildB() { }

        [Transition(InitialChildState.Outside, InitialChildTrigger.EnterParent, InitialChildState.Parent)]
        [Transition(InitialChildState.Parent_A, InitialChildTrigger.Switch, InitialChildState.Parent_B)]
        [Transition(InitialChildState.Parent, InitialChildTrigger.LeaveParent, InitialChildState.Outside)]
        private void Configure() { }
    }

    // Fluent API version
    [StateMachine(typeof(InitialChildState), typeof(InitialChildTrigger), EnableHierarchy = true)]
    public partial class InitialChildMachine_Fluent
    {
        public static void Configure()
        {
            // Define parent state with its initial child
            FSM.State(InitialChildState.Parent)
               .Initial(InitialChildState.Parent_A)
               .On(InitialChildTrigger.LeaveParent).GoTo(InitialChildState.Outside);

            // Define child states
            FSM.State(InitialChildState.Parent_A)
               .ChildOf(InitialChildState.Parent)
               .On(InitialChildTrigger.Switch).GoTo(InitialChildState.Parent_B);

            FSM.State(InitialChildState.Parent_B)
               .ChildOf(InitialChildState.Parent);

            // Define outside state
            FSM.State(InitialChildState.Outside)
               .On(InitialChildTrigger.EnterParent).GoTo(InitialChildState.Parent);
        }
    }
}