using Abstractions.Attributes;
using System.Threading.Tasks;
using Dsl;

namespace ParserComparison.Tests
{
    // Simple test to verify Fluent HSM parsing
    [StateMachine(typeof(TestState), typeof(TestTrigger), EnableHierarchy = true)]
    public partial class TestTinyHsm
    {
        public enum TestState { Outside, Menu, Menu_Item }
        public enum TestTrigger { Enter }

        private static void Configure()
        {
            FSM.State(TestState.Menu)
                .HistoryShallow()
                .OnEntryAsync(nameof(OnMenuEntryAsync))
                .Initial(TestState.Menu_Item);
                
            FSM.State(TestState.Menu_Item)
                .ChildOf(TestState.Menu);
            
            FSM.State(TestState.Outside);
            
            FSM.At(TestState.Outside)
                .On(TestTrigger.Enter)
                .GoTo(TestState.Menu);
        }

        private ValueTask OnMenuEntryAsync() => ValueTask.CompletedTask;
    }
}