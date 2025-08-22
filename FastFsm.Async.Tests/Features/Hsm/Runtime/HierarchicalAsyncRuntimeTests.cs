
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Xunit;

namespace  FastFsm.Async.Tests.Features.Hsm.Runtime
{
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
            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))] private void Parent() { }
            [State(S.Parent_A, Parent = S.Parent, IsInitial = true)] private void ChildA() { }
            [State(S.Parent_B, Parent = S.Parent)] private void ChildB() { }

            [Transition(S.Outside, T.EnterParent, S.Parent)]
            [Transition(S.Parent_A, T.Switch, S.Parent_B)]
            [Transition(S.Parent, T.LeaveParent, S.Outside)]
            private void Configure() { }

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
            [State(S.Menu, History = HistoryMode.Shallow, OnEntry = nameof(OnMenuEntryAsync))] private void Menu() { }
            [State(S.Menu_Main, Parent = S.Menu, IsInitial = true)] private void Main() { }
            [State(S.Menu_Settings, Parent = S.Menu)] private void Settings() { }

            [Transition(S.Outside, T.Enter, S.Menu)]
            [Transition(S.Menu_Main, T.Next, S.Menu_Settings)]
            [Transition(S.Menu_Settings, T.Back, S.Menu_Main)]
            [Transition(S.Menu, T.Exit, S.Outside)]
            private void Configure() { }

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
            [State(S.Work, History = HistoryMode.Deep, OnEntry = nameof(OnWorkEntryAsync))] private void Work() { }
            [State(S.Work_S1, Parent = S.Work, IsInitial = true)] private void S1() { }
            [State(S.Work_S1_Loading, Parent = S.Work_S1, IsInitial = true)] private void Loading() { }
            [State(S.Work_S1_Calc, Parent = S.Work_S1)] private void Calc() { }

            [Transition(S.Out, T.EnterWork, S.Work)]
            [Transition(S.Work_S1_Loading, T.Next, S.Work_S1_Calc)]
            [Transition(S.Work, T.Abort, S.Out)]
            private void Configure() { }

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

            [State(S.Parent)] private void Parent() { }
            [State(S.Child, Parent = S.Parent, IsInitial = true)] private void Child() { }

            // Parent internal (always present)
            [InternalTransition(S.Parent, T.Refresh, Action = nameof(ParentInternalAsync))]
            private void ParentInternals() { }

            // Child internal with guard
            [InternalTransition(S.Child, T.Refresh, Guard = nameof(UseChildInternalGuard), Action = nameof(ChildInternalAsync))]
            private void ChildInternals() { }

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

            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))] private void Parent() { }
            [State(S.Child, Parent = S.Parent, IsInitial = true)] private void Child() { }

            [Transition(S.Parent, T.Go, S.ParentDone, Priority = 200, Action = nameof(P))]
            [Transition(S.Child, T.Go, S.Child, Priority = 100, Action = nameof(C))]
            private void Configure() { }

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class ChildOverridesMachine
        {
            public List<string> Log { get; } = new();

            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))] private void Parent() { }
            [State(S.Child, Parent = S.Parent, IsInitial = true)] private void Child() { }

            [Transition(S.Parent, T.Go, S.Parent, Priority = 100, Action = nameof(P))]
            [Transition(S.Child, T.Go, S.Child, Priority = 100, Action = nameof(C))]
            private void Configure() { }

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        [StateMachine(typeof(S), typeof(T))]
        public partial class SourceOrderTieMachine
        {
            public List<string> Log { get; } = new();
            [State(S.A, OnEntry = nameof(OnAEntryAsync))] private void AState() { }
            [Transition(S.A, T.Go, S.B, Priority = 0, Action = nameof(First))]
            [Transition(S.A, T.Go, S.C, Priority = 0, Action = nameof(Second))]
            private void Configure() { }

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
            [State(S.Parent, OnEntry = nameof(OnParentEntryAsync))] private void Parent() { }
            [State(S.Parent_A, Parent = S.Parent, IsInitial = true)] private void A() { }
            [State(S.Parent_B, Parent = S.Parent)] private void B() { }

            // Parent‑level transition that applies from any child
            [Transition(S.Parent, T.Leave, S.Outside)]
            // Child‑only transition
            [Transition(S.Parent_A, T.Next, S.Parent_B)]
            // Enter composite from outside
            [Transition(S.Outside, T.Enter, S.Parent)]
            private void Configure() { }

            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }
    }
}
