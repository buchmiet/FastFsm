
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;
using Xunit;

namespace  FastFsm.Async.Tests.Features.Hsm.Runtime
{
    // 1) Auto‑descend to initial child + basic parent/child wiring (async)
    public partial class AsyncInitialChildTests
    {
        [Fact]
        public async Task Transition_ToCompositeParent_Enters_ItsInitialChild()
        {
            var m = new InitialChildMachineFluentFsm(S.Outside);
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
        public partial class InitialChildMachineFluentFsm
        {
            private static void Configure() => FSM
                .State(S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                .State(S.Parent_A).Parent(S.Parent).IsInitial()
                .State(S.Parent_B).Parent(S.Parent)
                .State(S.Outside)
                    .On(T.EnterParent).GoTo(S.Parent)
                .State(S.Parent_A)
                    .On(T.Switch).GoTo(S.Parent_B)
                .State(S.Parent)
                    .On(T.LeaveParent).GoTo(S.Outside);

            private async Task OnParentEntryAsync() => await Task.Yield();
        }
    }

    // 2) Shallow history remembers last child (async)
    public partial class AsyncShallowHistoryTests
    {
        [Fact]
        public async Task Reentering_Parent_With_ShallowHistory_Restores_LastChild()
        {
            var m = new ShallowHistoryMachineFluentFsm(S.Outside);
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
        public partial class ShallowHistoryMachineFluentFsm
        {
            private static void Configure() => FSM
                .State(S.Menu).WithHistory(HistoryMode.Shallow)
                    .OnEntryAsync(nameof(OnMenuEntryAsync))
                .State(S.Menu_Main).Parent(S.Menu).IsInitial()
                .State(S.Menu_Settings).Parent(S.Menu)
                .State(S.Outside)
                    .On(T.Enter).GoTo(S.Menu)
                .State(S.Menu_Main)
                    .On(T.Next).GoTo(S.Menu_Settings)
                .State(S.Menu_Settings)
                    .On(T.Back).GoTo(S.Menu_Main)
                .State(S.Menu)
                    .On(T.Exit).GoTo(S.Outside);

            private async Task OnMenuEntryAsync() => await Task.CompletedTask;
        }
    }

    // 3) Deep history restores entire path (async)
    public partial class AsyncDeepHistoryTests
    {
        [Fact]
        public async Task DeepHistory_Restores_LeafPath_After_Reentering()
        {
            var m = new DeepHistoryMachineFluentFsm(S.Out);
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
        public partial class DeepHistoryMachineFluentFsm
        {
            private static void Configure() => FSM
                .State(S.Work).WithHistory(HistoryMode.Deep)
                    .OnEntryAsync(nameof(OnWorkEntryAsync))
                .State(S.Work_S1).Parent(S.Work).IsInitial()
                .State(S.Work_S1_Loading).Parent(S.Work_S1).IsInitial()
                .State(S.Work_S1_Calc).Parent(S.Work_S1)
                .State(S.Out)
                    .On(T.EnterWork).GoTo(S.Work)
                .State(S.Work_S1_Loading)
                    .On(T.Next).GoTo(S.Work_S1_Calc)
                .State(S.Work)
                    .On(T.Abort).GoTo(S.Out);

            private async Task OnWorkEntryAsync() => await Task.CompletedTask;
        }
    }

    // 4) Internal transitions: no state change and no entry/exit (async)
    public partial class AsyncInternalTransitionTests
    {
        [Fact]
        public async Task Internal_OnParent_Executes_Action_Without_ExitOrEntry()
        {
            var m = new InternalMachineFluentFsm(S.Parent);
            await m.StartAsync(); // auto enters Child
            m.Log.Clear();

            await m.FireAsync(T.Refresh);

            m.CurrentState.ShouldBe(S.Child); // state unchanged
            m.Log.ShouldBe(["ParentInternal"]);
        }

        [Fact]
        public async Task Internal_OnChild_Overrides_Parent_When_PriorityEqual()
        {
            var m = new InternalMachineFluentFsm(S.Parent) { UseChildInternal = true };
            await m.StartAsync();
            m.Log.Clear();

            await m.FireAsync(T.Refresh);

            m.CurrentState.ShouldBe(S.Child);
            m.Log.ShouldBe(["ChildInternal"]);
        }

        public enum S { Parent, Child }
        public enum T { Refresh }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InternalMachineFluentFsm
        {
            public List<string> Log { get; } = new();
            public bool UseChildInternal { get; set; }

            private static void Configure() => FSM
                .State(S.Parent)
                    .OnInternal(T.Refresh)
                        .ActionAsync(nameof(ParentInternalAsync))
                        .Internal()
                .State(S.Child).Parent(S.Parent).IsInitial()
                    .OnInternal(T.Refresh)
                        .Guard(nameof(UseChildInternalGuard))
                        .ActionAsync(nameof(ChildInternalAsync))
                        .Internal();

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
            var m = new PriorityMachineFluentFsm(S.Parent);
            await m.StartAsync(); // enters Child
            await m.FireAsync(T.Go);
            m.CurrentState.ShouldBe(S.ParentDone); // parent wins due to higher priority
            m.Log.ShouldBe(["Parent"]);
        }

        [Fact]
        public async Task ChildOverridesParent_When_PrioEqual()
        {
            var m = new ChildOverridesMachineFluentFsm(S.Parent);
            await m.StartAsync();
            await m.FireAsync(T.Go);
            m.CurrentState.ShouldBe(S.Child); // child wins over parent at equal priority
            m.Log.ShouldBe(["Child"]);
        }

        [Fact]
        public async Task SourceOrder_Tie_Breaks_By_First_Declared()
        {
            var m = new SourceOrderTieMachineFluentFsm(S.A);
            await m.StartAsync();
            await m.FireAsync(T.Go);
            m.CurrentState.ShouldBe(S.B); // first declared wins
            m.Log.ShouldBe(["First"]);
        }

        public enum S { Parent, Child, ParentDone, A, B, C }
        public enum T { Go }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class PriorityMachineFluentFsm
        {
            public List<string> Log { get; } = new();

            private static void Configure() => FSM
                .State(S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                    .On(T.Go).GoTo(S.ParentDone).ActionAsync(nameof(P)).Priority(200)
                .State(S.Child).Parent(S.Parent).IsInitial()
                    .On(T.Go).GoTo(S.Child).ActionAsync(nameof(C)).Priority(100);

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class ChildOverridesMachineFluentFsm
        {
            public List<string> Log { get; } = new();

            private static void Configure() => FSM
                .State(S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                    .On(T.Go).GoTo(S.Parent).ActionAsync(nameof(P)).Priority(100)
                .State(S.Child).Parent(S.Parent).IsInitial()
                    .On(T.Go).GoTo(S.Child).ActionAsync(nameof(C)).Priority(100);

            private async Task P() { await Task.Yield(); Log.Add("Parent"); }
            private async Task C() { await Task.Yield(); Log.Add("Child"); }
            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }

        [StateMachine(typeof(S), typeof(T))]
        public partial class SourceOrderTieMachineFluentFsm
        {
            public List<string> Log { get; } = new();

            private static void Configure() => FSM
                .State(S.A)
                    .OnEntryAsync(nameof(OnAEntryAsync))
                    .On(T.Go).GoTo(S.B).ActionAsync(nameof(First)).Priority(0)
                    .On(T.Go).GoTo(S.C).ActionAsync(nameof(Second)).Priority(0);

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
            var m = new InheritanceMachineFluentFsm(S.Outside);
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
            var m = new InheritanceMachineFluentFsm(S.Outside);
            await m.StartAsync();

            await m.FireAsync(T.Enter); // now in Parent_A
            m.IsIn(S.Parent).ShouldBeTrue();

            await m.FireAsync(T.Leave);
            m.IsIn(S.Parent).ShouldBeFalse();
        }

        [Fact]
        public async Task DumpActivePath_Contains_Parent_And_Leaf()
        {
            var m = new InheritanceMachineFluentFsm(S.Outside);
            await m.StartAsync();
            await m.FireAsync(T.Enter); // Parent → initial child

            var path = m.DumpActivePath();
            path.ShouldContain("Parent");
            path.ShouldContain("Parent_A");
        }

        public enum S { Outside, Parent, Parent_A, Parent_B }
        public enum T { Enter, Next, Leave }

        [StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InheritanceMachineFluentFsm
        {
            private static void Configure() => FSM
                .State(S.Parent)
                    .OnEntryAsync(nameof(OnParentEntryAsync))
                    .On(T.Leave).GoTo(S.Outside)
                .State(S.Parent_A).Parent(S.Parent).IsInitial()
                    .On(T.Next).GoTo(S.Parent_B)
                .State(S.Parent_B).Parent(S.Parent)
                .State(S.Outside)
                    .On(T.Enter).GoTo(S.Parent);

            private async Task OnParentEntryAsync() => await Task.CompletedTask;
        }
    }
}
