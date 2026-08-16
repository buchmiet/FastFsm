using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests
{
    // Test enums for ShallowHistory machine
    public enum ShallowHistoryState 
    { 
        Outside, 
        Menu, 
        Menu_Main, 
        Menu_Settings 
    }
    
    public enum ShallowHistoryTrigger 
    { 
        Enter, 
        Next, 
        Back, 
        Exit 
    }

    // Attribute-based version (legacy)
    [StateMachine(typeof(ShallowHistoryState), typeof(ShallowHistoryTrigger), EnableHierarchy = true)]
    public partial class ShallowHistoryMachine_Attributes
    {
        [State(ShallowHistoryState.Menu, History = HistoryMode.Shallow)] 
        private void Menu() { }
        
        [State(ShallowHistoryState.Menu_Main, Parent = ShallowHistoryState.Menu, IsInitial = true)] 
        private void Main() { }
        
        [State(ShallowHistoryState.Menu_Settings, Parent = ShallowHistoryState.Menu)] 
        private void Settings() { }

        [Transition(ShallowHistoryState.Outside, ShallowHistoryTrigger.Enter, ShallowHistoryState.Menu)]
        [Transition(ShallowHistoryState.Menu_Main, ShallowHistoryTrigger.Next, ShallowHistoryState.Menu_Settings)]
        [Transition(ShallowHistoryState.Menu_Settings, ShallowHistoryTrigger.Back, ShallowHistoryState.Menu_Main)]
        [Transition(ShallowHistoryState.Menu, ShallowHistoryTrigger.Exit, ShallowHistoryState.Outside)]
        private void Configure() { }
    }

    // Fluent API version
    [StateMachine(typeof(ShallowHistoryState), typeof(ShallowHistoryTrigger), EnableHierarchy = true)]
    public partial class ShallowHistoryMachine_Fluent
    {
        public static void Configure()
        {
            // Define parent state with shallow history and initial child
            FSM.State(ShallowHistoryState.Menu)
               .Initial(ShallowHistoryState.Menu_Main)
               .HistoryShallow()
               .On(ShallowHistoryTrigger.Exit).GoTo(ShallowHistoryState.Outside);

            // Define child states
            FSM.State(ShallowHistoryState.Menu_Main)
               .ChildOf(ShallowHistoryState.Menu)
               .On(ShallowHistoryTrigger.Next).GoTo(ShallowHistoryState.Menu_Settings);

            FSM.State(ShallowHistoryState.Menu_Settings)
               .ChildOf(ShallowHistoryState.Menu)
               .On(ShallowHistoryTrigger.Back).GoTo(ShallowHistoryState.Menu_Main);

            // Define outside state
            FSM.State(ShallowHistoryState.Outside)
               .On(ShallowHistoryTrigger.Enter).GoTo(ShallowHistoryState.Menu);
        }
    }
}