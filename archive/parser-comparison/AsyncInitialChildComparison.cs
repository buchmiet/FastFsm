using System.Threading.Tasks;
using Abstractions.Attributes;
using Dsl;

namespace ParserComparison.Tests;

// Legacy version
public partial class AsyncInitialChildTests
{
    public enum S { Outside, Parent, Parent_A, Parent_B }
    public enum T { EnterParent, Switch, LeaveParent }

    [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
    public partial class InitialChildMachine
    {
        [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))]
        [State(S.Parent_A, Parent = S.Parent, IsInitial = true)]
        [State(S.Parent_B, Parent = S.Parent)]
        [State(S.Outside)]
        private void ConfigureStates() { }

        [Transition(S.Outside, T.EnterParent, S.Parent)]
        [Transition(S.Parent_A, T.Switch, S.Parent_B)]
        [Transition(S.Parent, T.LeaveParent, S.Outside)]
        private void ConfigureTransitions() { }

        private async Task OnParentEntryAsync() => await Task.Yield();
    }
}

// Fluent version
public partial class AsyncInitialChildTestsFluent
{
    [StateMachine(typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.T), EnableHierarchy = true)]
    public partial class InitialChildMachineFluentFsm
    {
        private static void Configure()
        {
            FSM.State(AsyncInitialChildTests.S.Parent)
                .OnEntryAsync(nameof(OnParentEntryAsync))
                .Initial(AsyncInitialChildTests.S.Parent_A);
                
            FSM.State(AsyncInitialChildTests.S.Parent_A)
                .ChildOf(AsyncInitialChildTests.S.Parent);
                
            FSM.State(AsyncInitialChildTests.S.Parent_B)
                .ChildOf(AsyncInitialChildTests.S.Parent);
                
            FSM.State(AsyncInitialChildTests.S.Outside);
            
            FSM.At(AsyncInitialChildTests.S.Outside)
                .On(AsyncInitialChildTests.T.EnterParent)
                .GoTo(AsyncInitialChildTests.S.Parent);
                
            FSM.At(AsyncInitialChildTests.S.Parent_A)
                .On(AsyncInitialChildTests.T.Switch)
                .GoTo(AsyncInitialChildTests.S.Parent_B);
                
            FSM.At(AsyncInitialChildTests.S.Parent)
                .On(AsyncInitialChildTests.T.LeaveParent)
                .GoTo(AsyncInitialChildTests.S.Outside);
        }

        private async Task OnParentEntryAsync() => await Task.Yield();
    }
}