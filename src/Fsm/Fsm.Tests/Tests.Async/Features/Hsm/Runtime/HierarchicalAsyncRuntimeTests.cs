
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Xunit;
using Dsl;

namespace Tests.Async.Features.Hsm.Runtime;
    // 1) Auto‑descend to initial child + basic parent/child wiring (async)
    public partial class AsyncInitialChildTests
    {
        [Fact]
        public async Task Transition_ToCompositeParent_Enters_ItsInitialChild()
        {
            var m = new InitialChildMachine(S.Outside);
            await m.StartAsync();

            m.CurrentState.ShouldBe(S.Outside);

            await m.FireAsync(T.EnterParent);
            m.CurrentState.ShouldBe(S.Parent_A); // auto‑descend to initial child

            await m.FireAsync(T.Switch);
            m.CurrentState.ShouldBe(S.Parent_B);

            await m.FireAsync(T.LeaveParent);
            m.CurrentState.ShouldBe(S.Outside);
        }

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

    // 2) Shallow history remembers last child (async)
    public partial class AsyncShallowHistoryTests
    {
        [Fact]
        public async Task Reentering_Parent_With_ShallowHistory_Restores_LastChild()
        {
            var m = new ShallowHistoryMachine(S.Outside);
            await m.StartAsync();

            // Enter parent → initial child
            await m.FireAsync(T.Enter);
            m.CurrentState.ShouldBe(S.Menu_Main);

            // Move to another child
            await m.FireAsync(T.Next);
            m.CurrentState.ShouldBe(S.Menu_Settings);

            // Exit composite
            await m.FireAsync(T.Exit);
            m.CurrentState.ShouldBe(S.Outside);

            // Re‑enter → shallow history brings us back to Settings
            await m.FireAsync(T.Enter);
            m.CurrentState.ShouldBe(S.Menu_Settings);
        }

        public enum S { Outside, Menu, Menu_Main, Menu_Settings }
        public enum T { Enter, Next, Back, Exit }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class ShallowHistoryMachine
        {
            [State(S.Menu, History = Abstractions.Attributes.HistoryMode.Shallow, OnEntry = nameof(OnMenuEntryAsync))]
            [State(S.Menu_Main, Parent = S.Menu, IsInitial = true)]
            [State(S.Menu_Settings, Parent = S.Menu)]
            [State(S.Outside)]
            private void ConfigureStates() { }

            [Transition(S.Outside, T.Enter, S.Menu)]
            [Transition(S.Menu_Main, T.Next, S.Menu_Settings)]
            [Transition(S.Menu_Settings, T.Back, S.Menu_Main)]
            [Transition(S.Menu, T.Exit, S.Outside)]
            private void ConfigureTransitions() { }

            private async Task OnMenuEntryAsync() => await Task.CompletedTask;
        }
    }

    // 3) Deep history restores entire path (async)
    public partial class AsyncDeepHistoryTests
    {
        [Fact]
        public async Task DeepHistory_Restores_LeafPath_After_Reentering()
        {
            var m = new DeepHistoryMachine(S.Out);
            await m.StartAsync();

            // Enter composite → auto path: Work → S1 (initial) → Loading (initial)
            await m.FireAsync(T.EnterWork);
            m.CurrentState.ShouldBe(S.Work_S1_Loading);

            // Move to deeper sibling leaf
            await m.FireAsync(T.Next);
            m.CurrentState.ShouldBe(S.Work_S1_Calc);

            // Exit composite to outside
            await m.FireAsync(T.Abort);
            m.CurrentState.ShouldBe(S.Out);

            // Re‑enter → deep history returns to the last leaf (Calc)
            await m.FireAsync(T.EnterWork);
            m.CurrentState.ShouldBe(S.Work_S1_Calc);
        }

        public enum S { Out, Work, Work_S1, Work_S1_Loading, Work_S1_Calc }
        public enum T { EnterWork, Next, Abort }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class DeepHistoryMachine
        {
            [State(S.Work, History = Abstractions.Attributes.HistoryMode.Deep, OnEntry = nameof(OnWorkEntryAsync))]
            [State(S.Work_S1, Parent = S.Work, IsInitial = true)]
            [State(S.Work_S1_Loading, Parent = S.Work_S1, IsInitial = true)]
            [State(S.Work_S1_Calc, Parent = S.Work_S1)]
            [State(S.Out)]
            private void ConfigureStates() { }

            [Transition(S.Out, T.EnterWork, S.Work)]
            [Transition(S.Work_S1_Loading, T.Next, S.Work_S1_Calc)]
            [Transition(S.Work, T.Abort, S.Out)]
            private void ConfigureTransitions() { }

            private async Task OnWorkEntryAsync() => await Task.CompletedTask;
        }
    }

    // 4) Internal transitions: no state change and no entry/exit (async)
    public partial class AsyncInternalTransitionTests
    {
        [Fact]
        public async Task Internal_OnParent_Executes_Action_Without_ExitOrEntry()
        {
            var m = new InternalMachine(S.Parent);
            await m.StartAsync(); // auto enters Child
            m.Log.Clear();

            await m.FireAsync(T.Refresh);

            m.CurrentState.ShouldBe(S.Child); // state unchanged
            m.Log.ShouldBe(["ParentInternal"]);
        }

        [Fact]
        public async Task Internal_OnChild_Overrides_Parent_When_PriorityEqual()
        {
            var m = new InternalMachine(S.Parent) { UseChildInternal = true };
            await m.StartAsync();
            m.Log.Clear();

            await m.FireAsync(T.Refresh);

            m.CurrentState.ShouldBe(S.Child);
            m.Log.ShouldBe(["ChildInternal"]);
        }

        public enum S { Parent, Child }
        public enum T { Refresh }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InternalMachine
        {
            public List<string> Log { get; } = new();
            public bool UseChildInternal { get; set; }

            [State(S.Parent)]
            [State(S.Child, Parent = S.Parent, IsInitial = true)]
            private void ConfigureStates() { }

            [InternalTransition(S.Parent, T.Refresh, nameof(ParentInternalAsync))]
            [InternalTransition(S.Child, T.Refresh, nameof(ChildInternalAsync), Guard = nameof(UseChildInternalGuard))]
            private void ConfigureInternalTransitions() { }

            private async Task ParentInternalAsync() { await Task.Yield(); Log.Add("ParentInternal"); }
            private async Task ChildInternalAsync() { await Task.Yield(); Log.Add("ChildInternal"); }
            private bool UseChildInternalGuard() => UseChildInternal;
        }
    }

    // 5) Resolution order: Priority → Child over Parent → Source order (async)
    public partial class AsyncResolutionOrderTests
    {
        [Fact]
        public async Task HigherPriority_Wins_Even_If_Parent()
        {
            var m = new PriorityMachine(S.Parent);
            await m.StartAsync(); // enters Child
            await m.FireAsync(T.Go);
            m.CurrentState.ShouldBe(S.ParentDone); // parent wins due to higher priority
            m.Log.ShouldBe(["Parent"]);
        }

        [Fact]
        public async Task ChildOverridesParent_When_PrioEqual()
        {
            var m = new ChildOverridesMachine(S.Parent);
            await m.StartAsync();
            await m.FireAsync(T.Go);
            m.CurrentState.ShouldBe(S.Child); // child wins over parent at equal priority
            m.Log.ShouldBe(["Child"]);
        }

        [Fact]
        public async Task SourceOrder_Tie_Breaks_By_First_Declared()
        {
            var m = new SourceOrderTieMachine(S.A);
            await m.StartAsync();
            await m.FireAsync(T.Go);
            m.CurrentState.ShouldBe(S.B); // first declared wins
            m.Log.ShouldBe(["First"]);
        }

        public enum S { Parent, Child, ParentDone, A, B, C }
        public enum T { Go }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class PriorityMachine
        {
            public List<string> Log { get; } = new();

            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))]
            [State(S.Child, Parent = S.Parent, IsInitial = true)]
            [State(S.ParentDone)]
            private void ConfigureStates() { }

            [Transition(S.Parent, T.Go, S.ParentDone, Action = nameof(P), Priority = 200)]
            [Transition(S.Child, T.Go, S.Child, Action = nameof(C), Priority = 100)]
            private void ConfigureTransitions() { }

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class ChildOverridesMachine
        {
            public List<string> Log { get; } = new();

            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))]
            [State(S.Child, Parent = S.Parent, IsInitial = true)]
            private void ConfigureStates() { }

            [Transition(S.Parent, T.Go, S.Parent, Action = nameof(P), Priority = 100)]
            [Transition(S.Child, T.Go, S.Child, Action = nameof(C), Priority = 100)]
            private void ConfigureTransitions() { }

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        [StateMachine(typeof(S), typeof(T))]
        public partial class SourceOrderTieMachine
        {
            public List<string> Log { get; } = new();

            [State(S.A, OnEntry = nameof(OnAEntryAsync))]
            [State(S.B)]
            [State(S.C)]
            private void ConfigureStates() { }

            [Transition(S.A, T.Go, S.B, Action = nameof(First), Priority = 0)]
            [Transition(S.A, T.Go, S.C, Action = nameof(Second), Priority = 0)]
            private void ConfigureTransitions() { }

            private async Task First() { await Task.Yield(); Log.Add("First"); }
            private async Task Second() { await Task.Yield(); Log.Add("Second"); }
            private async Task OnAEntryAsync() => await Task.CompletedTask;
        }
    }

    // 6) Inheritance + GetPermittedTriggers/CanFire + IsIn (async)
    public partial class AsyncInheritanceAndIntrospectionTests
    {
        [Fact]
        public async Task Child_Inherits_Parent_Transitions_And_PermittedTriggers_Unions()
        {
            var m = new InheritanceMachine(S.Outside);
            await m.StartAsync();

            // Enter the composite parent
            await m.FireAsync(T.Enter);
            m.CurrentState.ShouldBe(S.Parent_A);

            var permitted = await m.GetPermittedTriggersAsync();
            permitted.ShouldContain(T.Leave); // from parent
            permitted.ShouldContain(T.Next);  // from child
            (await m.CanFireAsync(T.Leave)).ShouldBeTrue();

            await m.FireAsync(T.Leave);
            m.CurrentState.ShouldBe(S.Outside);
        }

        [Fact]
        public async Task IsIn_Reports_Correctly()
        {
            var m = new InheritanceMachine(S.Outside);
            await m.StartAsync();

            await m.FireAsync(T.Enter); // now in Parent_A
            m.IsIn(S.Parent).ShouldBeTrue();

            await m.FireAsync(T.Leave);
            m.IsIn(S.Parent).ShouldBeFalse();
        }

        [Fact]
        public async Task DumpActivePath_Contains_Parent_And_Leaf()
        {
            var m = new InheritanceMachine(S.Outside);
            await m.StartAsync();
            await m.FireAsync(T.Enter); // Parent → initial child

            var path = m.DumpActivePath();
            path.ShouldContain("Parent");
            path.ShouldContain("Parent_A");
        }

        public enum S { Outside, Parent, Parent_A, Parent_B }
        public enum T { Enter, Next, Leave }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InheritanceMachine
        {
            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))]
            [State(S.Parent_A, Parent = S.Parent, IsInitial = true)]
            [State(S.Parent_B, Parent = S.Parent)]
            [State(S.Outside)]
            private void ConfigureStates() { }

            [Transition(S.Parent, T.Leave, S.Outside)]
            [Transition(S.Parent_A, T.Next, S.Parent_B)]
            [Transition(S.Outside, T.Enter, S.Parent)]
            private void ConfigureTransitions() { }

            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }
    }

    #region Fluent API Versions

    // 1) InitialChildMachineFluentFsm
    public partial class AsyncInitialChildTestsFluent
    {
        [StateMachine(typeof(AsyncInitialChildTests.S), typeof(AsyncInitialChildTests.T), EnableHierarchy = true)]
        public partial class InitialChildMachineFluentFsm
        {
            private void Configure()
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

    // 2) ShallowHistoryMachineFluentFsm
    public partial class AsyncShallowHistoryTestsFluent
    {
        [StateMachine(typeof(AsyncShallowHistoryTests.S), typeof(AsyncShallowHistoryTests.T), EnableHierarchy = true)]
        public partial class ShallowHistoryMachineFluentFsm
        {
            private void Configure()
            {
                FSM.State(AsyncShallowHistoryTests.S.Menu)
                    .HistoryShallow()
                    .OnEntryAsync(nameof(MenuEntryAsync))
                    .Initial(AsyncShallowHistoryTests.S.Menu_Main);
                    
                FSM.State(AsyncShallowHistoryTests.S.Menu_Main)
                    .ChildOf(AsyncShallowHistoryTests.S.Menu);
                    
                FSM.State(AsyncShallowHistoryTests.S.Menu_Settings)
                    .ChildOf(AsyncShallowHistoryTests.S.Menu);
                    
                FSM.State(AsyncShallowHistoryTests.S.Outside);
                
                FSM.At(AsyncShallowHistoryTests.S.Outside)
                    .On(AsyncShallowHistoryTests.T.Enter)
                    .GoTo(AsyncShallowHistoryTests.S.Menu);
                    
                FSM.At(AsyncShallowHistoryTests.S.Menu_Main)
                    .On(AsyncShallowHistoryTests.T.Next)
                    .GoTo(AsyncShallowHistoryTests.S.Menu_Settings);
                    
                FSM.At(AsyncShallowHistoryTests.S.Menu)
                    .On(AsyncShallowHistoryTests.T.Exit)
                    .GoTo(AsyncShallowHistoryTests.S.Outside);
            }

            private async ValueTask MenuEntryAsync() => await Task.Yield();
        }
    }

    // 3) DeepHistoryMachineFluentFsm
    public partial class AsyncDeepHistoryTestsFluent
    {
        [StateMachine(typeof(AsyncDeepHistoryTests.S), typeof(AsyncDeepHistoryTests.T), EnableHierarchy = true)]
        public partial class DeepHistoryMachineFluentFsm
        {
            private void Configure()
            {
                FSM.State(AsyncDeepHistoryTests.S.Work)
                    .HistoryDeep()
                    .OnEntryAsync(nameof(OnWorkEntryAsync))
                    .Initial(AsyncDeepHistoryTests.S.Work_S1);
                    
                FSM.State(AsyncDeepHistoryTests.S.Work_S1)
                    .ChildOf(AsyncDeepHistoryTests.S.Work)
                    .Initial(AsyncDeepHistoryTests.S.Work_S1_Loading);
                    
                FSM.State(AsyncDeepHistoryTests.S.Work_S1_Loading)
                    .ChildOf(AsyncDeepHistoryTests.S.Work_S1);
                    
                FSM.State(AsyncDeepHistoryTests.S.Work_S1_Calc)
                    .ChildOf(AsyncDeepHistoryTests.S.Work_S1);
                    
                FSM.State(AsyncDeepHistoryTests.S.Out);
                
                FSM.At(AsyncDeepHistoryTests.S.Out)
                    .On(AsyncDeepHistoryTests.T.EnterWork)
                    .GoTo(AsyncDeepHistoryTests.S.Work);
                    
                FSM.At(AsyncDeepHistoryTests.S.Work_S1_Loading)
                    .On(AsyncDeepHistoryTests.T.Next)
                    .GoTo(AsyncDeepHistoryTests.S.Work_S1_Calc);
                    
                FSM.At(AsyncDeepHistoryTests.S.Work)
                    .On(AsyncDeepHistoryTests.T.Abort)
                    .GoTo(AsyncDeepHistoryTests.S.Out);
            }

            private async Task OnWorkEntryAsync() => await Task.Yield();
        }
    }

    // 4) InternalMachineFluentFsm
    public partial class AsyncInternalTransitionTestsFluent
    {
        [StateMachine(typeof(AsyncInternalTransitionTests.S), typeof(AsyncInternalTransitionTests.T), EnableHierarchy = true)]
        public partial class InternalMachineFluentFsm
        {
            private void Configure()
            {
                FSM.State(AsyncInternalTransitionTests.S.Parent)
                    .Initial(AsyncInternalTransitionTests.S.Child);
                    
                FSM.State(AsyncInternalTransitionTests.S.Child)
                    .ChildOf(AsyncInternalTransitionTests.S.Parent);
                
                // Internal transitions matching the Legacy machine
                FSM.At(AsyncInternalTransitionTests.S.Parent)
                    .OnInternal(AsyncInternalTransitionTests.T.Refresh)
                    .ActionAsync(nameof(ParentInternalAsync));
                    
                FSM.At(AsyncInternalTransitionTests.S.Child)
                    .OnInternal(AsyncInternalTransitionTests.T.Refresh)
                    .Guard(nameof(UseChildInternalGuard))
                    .ActionAsync(nameof(ChildInternalAsync));
            }

            public List<string> Log { get; } = new();
            public bool UseChildInternal { get; set; }
            
            private async Task ParentInternalAsync() { await Task.Yield(); Log.Add("ParentInternal"); }
            private async Task ChildInternalAsync() { await Task.Yield(); Log.Add("ChildInternal"); }
            private bool UseChildInternalGuard() => UseChildInternal;
        }
    }

    // 5-7) Resolution Order Tests
    public partial class AsyncResolutionOrderTestsFluent
    {
        // 5) PriorityMachineFluentFsm
        [StateMachine(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), EnableHierarchy = true)]
        public partial class PriorityMachineFluentFsm
        {
            public List<string> Log { get; } = new();
            
            private void Configure()
            {
                FSM.State(AsyncResolutionOrderTests.S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                    .Initial(AsyncResolutionOrderTests.S.Child);
                    
                FSM.State(AsyncResolutionOrderTests.S.Child)
                    .ChildOf(AsyncResolutionOrderTests.S.Parent);
                    
                FSM.State(AsyncResolutionOrderTests.S.ParentDone);
                
                // Parent has higher priority
                FSM.At(AsyncResolutionOrderTests.S.Parent)
                    .On(AsyncResolutionOrderTests.T.Go)
                    .ActionAsync(nameof(P))
                    .Priority(200)
                    .GoTo(AsyncResolutionOrderTests.S.ParentDone);
                    
                // Child has lower priority
                FSM.At(AsyncResolutionOrderTests.S.Child)
                    .On(AsyncResolutionOrderTests.T.Go)
                    .ActionAsync(nameof(C))
                    .Priority(100)
                    .GoTo(AsyncResolutionOrderTests.S.Child);
            }

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        // 6) ChildOverridesMachineFluentFsm
        [StateMachine(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T), EnableHierarchy = true)]
        public partial class ChildOverridesMachineFluentFsm
        {
            public List<string> Log { get; } = new();
            
            private void Configure()
            {
                FSM.State(AsyncResolutionOrderTests.S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                    .Initial(AsyncResolutionOrderTests.S.Child);
                    
                FSM.State(AsyncResolutionOrderTests.S.Child)
                    .ChildOf(AsyncResolutionOrderTests.S.Parent);
                
                // Parent handles trigger
                FSM.At(AsyncResolutionOrderTests.S.Parent)
                    .On(AsyncResolutionOrderTests.T.Go)
                    .ActionAsync(nameof(P))
                    .Priority(100)
                    .GoTo(AsyncResolutionOrderTests.S.Parent);
                    
                // Child also handles it - should override
                FSM.At(AsyncResolutionOrderTests.S.Child)
                    .On(AsyncResolutionOrderTests.T.Go)
                    .ActionAsync(nameof(C))
                    .Priority(100)
                    .GoTo(AsyncResolutionOrderTests.S.Child);
            }

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        // 7) SourceOrderTieMachineFluentFsm
        [StateMachine(typeof(AsyncResolutionOrderTests.S), typeof(AsyncResolutionOrderTests.T))]
        public partial class SourceOrderTieMachineFluentFsm
        {
            public List<string> Log { get; } = new();
            
            private void Configure()
            {
                FSM.State(AsyncResolutionOrderTests.S.A)
                    .OnEntryAsync(nameof(OnAEntryAsync));
                FSM.State(AsyncResolutionOrderTests.S.B);
                FSM.State(AsyncResolutionOrderTests.S.C);
                
                // Two transitions with same priority - first wins
                FSM.At(AsyncResolutionOrderTests.S.A)
                    .On(AsyncResolutionOrderTests.T.Go)
                    .ActionAsync(nameof(First))
                    .Priority(0)
                    .GoTo(AsyncResolutionOrderTests.S.B);
                    
                FSM.At(AsyncResolutionOrderTests.S.A)
                    .On(AsyncResolutionOrderTests.T.Go)
                    .ActionAsync(nameof(Second))
                    .Priority(0)
                    .GoTo(AsyncResolutionOrderTests.S.C);
            }

            private async Task OnAEntryAsync() => await Task.CompletedTask;
            private async Task First() { Log.Add("First"); await Task.Yield(); }
            private async Task Second() { Log.Add("Second"); await Task.Yield(); }
        }
    }

    // 8) InheritanceMachineFluentFsm
    public partial class AsyncInheritanceAndIntrospectionTestsFluent
    {
        [StateMachine(typeof(AsyncInheritanceAndIntrospectionTests.S), typeof(AsyncInheritanceAndIntrospectionTests.T), EnableHierarchy = true)]
        public partial class InheritanceMachineFluentFsm
        {
            private void Configure()
            {
                FSM.State(AsyncInheritanceAndIntrospectionTests.S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                    .Initial(AsyncInheritanceAndIntrospectionTests.S.Parent_A);
                    
                FSM.State(AsyncInheritanceAndIntrospectionTests.S.Parent_A)
                    .ChildOf(AsyncInheritanceAndIntrospectionTests.S.Parent);
                    
                FSM.State(AsyncInheritanceAndIntrospectionTests.S.Parent_B)
                    .ChildOf(AsyncInheritanceAndIntrospectionTests.S.Parent);
                    
                FSM.State(AsyncInheritanceAndIntrospectionTests.S.Outside);
                
                FSM.At(AsyncInheritanceAndIntrospectionTests.S.Parent)
                    .On(AsyncInheritanceAndIntrospectionTests.T.Leave)
                    .GoTo(AsyncInheritanceAndIntrospectionTests.S.Outside);
                    
                FSM.At(AsyncInheritanceAndIntrospectionTests.S.Parent_A)
                    .On(AsyncInheritanceAndIntrospectionTests.T.Next)
                    .GoTo(AsyncInheritanceAndIntrospectionTests.S.Parent_B);
                    
                FSM.At(AsyncInheritanceAndIntrospectionTests.S.Outside)
                    .On(AsyncInheritanceAndIntrospectionTests.T.Enter)
                    .GoTo(AsyncInheritanceAndIntrospectionTests.S.Parent);
            }

            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }
    }

    #endregion
 
