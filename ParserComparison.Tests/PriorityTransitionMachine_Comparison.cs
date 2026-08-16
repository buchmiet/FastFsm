using Abstractions.Attributes;
using Dsl;
using System.Collections.Generic;

namespace ParserComparison.Tests
{
    // Test enums for Priority machine
    public enum PriorityState 
    { 
        Parent, 
        Parent_Child,
        ParentDone,
        ChildDone
    }
    
    public enum PriorityTrigger 
    { 
        Go,
        Process,
        Complete
    }

    // Attribute-based version (legacy)
    [StateMachine(typeof(PriorityState), typeof(PriorityTrigger), EnableHierarchy = true)]
    public partial class PriorityTransitionMachine_Attributes
    {
        public List<string> Log { get; } = new();

        [State(PriorityState.Parent)] 
        private void Parent() { }
        
        [State(PriorityState.Parent_Child, Parent = PriorityState.Parent, IsInitial = true)] 
        private void Child() { }
        
        [State(PriorityState.ParentDone)]
        private void ParentDone() { }
        
        [State(PriorityState.ChildDone)]
        private void ChildDone() { }

        // Higher priority on parent wins
        [Transition(PriorityState.Parent, PriorityTrigger.Go, PriorityState.ParentDone, Priority = 200, Action = nameof(ParentAction))]
        [Transition(PriorityState.Parent_Child, PriorityTrigger.Go, PriorityState.ChildDone, Priority = 100, Action = nameof(ChildAction))]
        
        // Equal priority - child overrides parent
        [Transition(PriorityState.Parent, PriorityTrigger.Process, PriorityState.ParentDone, Priority = 100)]
        [Transition(PriorityState.Parent_Child, PriorityTrigger.Process, PriorityState.ChildDone, Priority = 100)]
        
        // Low priority on parent still wins over no transition on child
        [Transition(PriorityState.Parent, PriorityTrigger.Complete, PriorityState.ParentDone, Priority = 10)]
        private void Configure() { }
        
        private void ParentAction() => Log.Add("Parent");
        private void ChildAction() => Log.Add("Child");
    }

    // Fluent API version
    [StateMachine(typeof(PriorityState), typeof(PriorityTrigger), EnableHierarchy = true)]
    public partial class PriorityTransitionMachine_Fluent
    {
        public List<string> Log { get; } = new();

        public static void Configure()
        {
            // Define parent state with initial child
            FSM.State(PriorityState.Parent)
               .Initial(PriorityState.Parent_Child)
               // Higher priority on parent wins
               .On(PriorityTrigger.Go).GoTo(PriorityState.ParentDone).Priority(200).Action(nameof(ParentAction))
               // Equal priority - child overrides parent
               .On(PriorityTrigger.Process).GoTo(PriorityState.ParentDone).Priority(100)
               // Low priority on parent still wins over no transition on child
               .On(PriorityTrigger.Complete).GoTo(PriorityState.ParentDone).Priority(10);

            // Define child state
            FSM.State(PriorityState.Parent_Child)
               .ChildOf(PriorityState.Parent)
               // Lower priority than parent
               .On(PriorityTrigger.Go).GoTo(PriorityState.ChildDone).Priority(100).Action(nameof(ChildAction))
               // Equal priority - child overrides parent
               .On(PriorityTrigger.Process).GoTo(PriorityState.ChildDone).Priority(100);

            // Define result states
            FSM.State(PriorityState.ParentDone);
            FSM.State(PriorityState.ChildDone);
        }
        
        private void ParentAction() => Log.Add("Parent");
        private void ChildAction() => Log.Add("Child");
    }
}