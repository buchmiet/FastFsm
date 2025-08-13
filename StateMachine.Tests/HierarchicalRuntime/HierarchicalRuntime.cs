using Abstractions.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace StateMachine.Tests.HierarchicalRuntime
{
    #region 1) Auto‑descend to initial child + basic parent/child wiring
    public partial class InitialChildTests
    {
        [Fact]
        public void Transition_ToCompositeParent_Enters_ItsInitialChild()
        {
            var m = new InitialChildMachine(S.Outside);
            m.Start();

            Assert.Equal(S.Outside, m.CurrentState);

            m.Fire(T.EnterParent);
            Assert.Equal(S.Parent_A, m.CurrentState); // auto‑descend to initial child

            m.Fire(T.Switch);
            Assert.Equal(S.Parent_B, m.CurrentState);

            m.Fire(T.LeaveParent);
            Assert.Equal(S.Outside, m.CurrentState);
        }

        public enum S { Outside, Parent, Parent_A, Parent_B }
        public enum T { EnterParent, Switch, LeaveParent }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InitialChildMachine
        {
            [State(S.Parent)] private void Parent() { }
            [State(S.Parent_A, Parent = S.Parent, IsInitial = true)] private void ChildA() { }
            [State(S.Parent_B, Parent = S.Parent)] private void ChildB() { }

            [Transition(S.Outside, T.EnterParent, S.Parent)]
            [Transition(S.Parent_A, T.Switch, S.Parent_B)]
            [Transition(S.Parent, T.LeaveParent, S.Outside)]
            private void Configure() { }
        }
    }
    #endregion

    #region 2) Shallow history remembers last child
    public partial class ShallowHistoryTests
    {
        [Fact]
        public void Reentering_Parent_With_ShallowHistory_Restores_LastChild()
        {
            var m = new ShallowHistoryMachine(S.Outside);
            m.Start();

            // Enter parent → initial child
            m.Fire(T.Enter);
            Assert.Equal(S.Menu_Main, m.CurrentState);

            // Move to another child
            m.Fire(T.Next);
            Assert.Equal(S.Menu_Settings, m.CurrentState);

            // Exit composite
            m.Fire(T.Exit);
            Assert.Equal(S.Outside, m.CurrentState);

            // Re‑enter → shallow history brings us back to Settings
            m.Fire(T.Enter);
            Assert.Equal(S.Menu_Settings, m.CurrentState);
        }

        public enum S { Outside, Menu, Menu_Main, Menu_Settings }
        public enum T { Enter, Next, Back, Exit }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class ShallowHistoryMachine
        {
            [State(S.Menu, History = HistoryMode.Shallow)] private void Menu() { }
            [State(S.Menu_Main, Parent = S.Menu, IsInitial = true)] private void Main() { }
            [State(S.Menu_Settings, Parent = S.Menu)] private void Settings() { }

            [Transition(S.Outside, T.Enter, S.Menu)]
            [Transition(S.Menu_Main, T.Next, S.Menu_Settings)]
            [Transition(S.Menu_Settings, T.Back, S.Menu_Main)]
            [Transition(S.Menu, T.Exit, S.Outside)]
            private void Configure() { }
        }
    }
    #endregion

    #region 3) Deep history restores entire path
    public partial class DeepHistoryTests
    {
        [Fact]
        public void DeepHistory_Arrays_Generated_Correctly()
        {
            // Use reflection to check generated arrays
            var type = typeof(DeepHistoryMachine);
            var parentField = type.GetField("s_parent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var initialField = type.GetField("s_initialChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var depthField = type.GetField("s_depth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var historyField = type.GetField("s_history", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            var parent = (int[])parentField.GetValue(null);
            var initial = (int[])initialField.GetValue(null);
            var depth = (int[])depthField.GetValue(null);
            var history = (System.Array)historyField.GetValue(null);
            
            // Expected: Out=0, Work=1, Work_S1=2, Work_S1_Loading=3, Work_S1_Calc=4
            Assert.Equal(new[] { -1, -1, 1, 2, 2 }, parent);
            Assert.Equal(new[] { -1, 2, 3, -1, -1 }, initial);
            Assert.Equal(new[] { 0, 0, 1, 2, 2 }, depth);
            
            // Verify history array: Only Work should have Deep history
            // Debug: Print actual values
            for (int i = 0; i < history.Length; i++)
            {
                Console.WriteLine($"s_history[{i}] = {history.GetValue(i)}");
            }
            
            Assert.Equal("None", history.GetValue(0).ToString());
            Assert.Equal("Deep", history.GetValue(1).ToString());
            Assert.Equal("None", history.GetValue(2).ToString());
            Assert.Equal("None", history.GetValue(3).ToString());
            Assert.Equal("None", history.GetValue(4).ToString());
        }

        [Fact]
        public void DeepHistory_Restores_LeafPath_After_Reentering()
        {
            var m = new DeepHistoryMachine(S.Out);
            m.Start();

            // Enter composite → auto path: Work → S1 (initial) → Loading (initial)
            m.Fire(T.EnterWork);
            Assert.Equal(S.Work_S1_Loading, m.CurrentState);

            // Move to deeper sibling leaf
            m.Fire(T.Next);
            Assert.Equal(S.Work_S1_Calc, m.CurrentState);

            // Exit composite to outside
            m.Fire(T.Abort);
            Assert.Equal(S.Out, m.CurrentState);

            // Re‑enter → deep history returns to the last leaf (Calc)
            m.Fire(T.EnterWork);
            Assert.Equal(S.Work_S1_Calc, m.CurrentState);
        }

        public enum S { Out, Work, Work_S1, Work_S1_Loading, Work_S1_Calc }
        public enum T { EnterWork, Next, Abort }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class DeepHistoryMachine
        {
            [State(S.Work, History = HistoryMode.Deep)] private void Work() { }
            [State(S.Work_S1, Parent = S.Work, IsInitial = true)] private void S1() { }
            [State(S.Work_S1_Loading, Parent = S.Work_S1, IsInitial = true)] private void Loading() { }
            [State(S.Work_S1_Calc, Parent = S.Work_S1)] private void Calc() { }

            [Transition(S.Out, T.EnterWork, S.Work)]
            [Transition(S.Work_S1_Loading, T.Next, S.Work_S1_Calc)]
            [Transition(S.Work, T.Abort, S.Out)]
            private void Configure() { }
        }
    }
    #endregion

    #region 4) Internal transitions: no state change and no entry/exit
    public partial class InternalTransitionTests
    {
        [Fact]
        public void Internal_OnParent_Executes_Action_Without_ExitOrEntry()
        {
            var m = new InternalMachine(S.Parent);
            m.Start(); // auto enters Child
            m.Log.Clear();

            m.Fire(T.Refresh);

            Assert.Equal(S.Child, m.CurrentState); // state unchanged
            Assert.Equal(new[] { "ParentInternal" }, m.Log);
        }

        [Fact]
        public void Internal_OnChild_Overrides_Parent_When_PriorityEqual()
        {
            var m = new InternalMachine(S.Parent) { UseChildInternal = true };
            m.Start();
            m.Log.Clear();

            m.Fire(T.Refresh);

            Assert.Equal(S.Child, m.CurrentState);
            Assert.Equal(new[] { "ChildInternal" }, m.Log);
        }

        public enum S { Parent, Child }
        public enum T { Refresh }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InternalMachine
        {
            public List<string> Log { get; } = new();
            public bool UseChildInternal { get; set; }

            [State(S.Parent, OnEntry = nameof(OnParentEntry))] private void Parent() { }
            [State(S.Child, Parent = S.Parent, IsInitial = true,
                OnEntry = nameof(OnChildEntry), OnExit = nameof(OnChildExit))]
            private void Child() { }

            // Parent internal (always present)
            [InternalTransition(S.Parent, T.Refresh, Action = nameof(ParentInternalAction))]
            private void ParentInternals() { }

            // Child internal (conditionally compiled in generator regardless of UseChildInternal flag),
            // but we decide at runtime which action to log.
            [InternalTransition(S.Child, T.Refresh, Guard = nameof(UseChildInternalGuard), Action = nameof(ChildInternalAction))]
            private void ChildInternals() { }
            private void ParentInternalAction() => Log.Add("ParentInternal");
            private void ChildInternalAction() => Log.Add("ChildInternal");
            private bool UseChildInternalGuard() => UseChildInternal;

            private void OnParentEntry() { }
            private void OnChildEntry() => Log.Add("OnEntryChild");
            private void OnChildExit() => Log.Add("OnExitChild");
        }
    }
    #endregion

    #region 5) Resolution order: Priority → Child over Parent → Source order
    public partial class ResolutionOrderTests
    {
        [Fact]
        public void HigherPriority_Wins_Even_If_Parent()
        {
            var m = new PriorityMachine(S.Parent);
            m.Start(); // enters Child
            m.Fire(T.Go);
            Assert.Equal(S.ParentDone, m.CurrentState); // parent wins due to higher priority
            Assert.Equal(new[] { "Parent" }, m.Log);
        }

        [Fact]
        public void ChildOverridesParent_When_PriorityEqual()
        {
            var m = new ChildOverridesMachine(S.Parent);
            m.Start();
            m.Fire(T.Go);
            Assert.Equal(S.Child, m.CurrentState); // self‑transition on child
            Assert.Equal(new[] { "Child" }, m.Log);
        }

        [Fact]
        public void SourceOrder_Breaks_Ties_Within_Same_State()
        {
            var m = new SourceOrderTieMachine(S.A);
            m.Start();
            m.Fire(T.Go);
            Assert.Equal(S.B, m.CurrentState); // first declared wins
            Assert.Equal(new[] { "First" }, m.Log);
        }

        public enum S { Parent, Child, ParentDone, A, B, C }
        public enum T { Go }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class PriorityMachine
        {
            public List<string> Log { get; } = new();

            [State(S.Parent)] private void Parent() { }
            [State(S.Child, Parent = S.Parent, IsInitial = true)] private void Child() { }

            [Transition(S.Parent, T.Go, S.ParentDone, Priority = 200, Action = nameof(P))]
            [Transition(S.Child, T.Go, S.Child, Priority = 100, Action = nameof(C))]
            private void Configure() { }

            private void P() => Log.Add("Parent");
            private void C() => Log.Add("Child");
        }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class ChildOverridesMachine
        {
            public List<string> Log { get; } = new();

            [State(S.Parent)] private void Parent() { }
            [State(S.Child, Parent = S.Parent, IsInitial = true)] private void Child() { }

            [Transition(S.Parent, T.Go, S.Parent, Priority = 100, Action = nameof(P))]
            [Transition(S.Child, T.Go, S.Child, Priority = 100, Action = nameof(C))]
            private void Configure() { }

            private void P() => Log.Add("Parent");
            private void C() => Log.Add("Child");
        }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T))]
        public partial class SourceOrderTieMachine
        {
            public List<string> Log { get; } = new();

            [Transition(S.A, T.Go, S.B, Priority = 0, Action = nameof(First))]
            [Transition(S.A, T.Go, S.C, Priority = 0, Action = nameof(Second))]
            private void Configure() { }

            private void First() => Log.Add("First");
            private void Second() => Log.Add("Second");
        }
    }
    #endregion

    #region 6) Inheritance + GetPermittedTriggers/CanFire + IsInHierarchy + DumpActivePath
    public partial  class InheritanceAndIntrospectionTests
    {
        [Fact]
        public void Child_Inherits_Parent_Transitions_And_PermittedTriggers_Unions()
        {
            var m = new InheritanceMachine(S.Outside);
            m.Start();

            // Enter the composite parent
            m.Fire(T.Enter);
            Assert.Equal(S.Parent_A, m.CurrentState);

            var permitted = m.GetPermittedTriggers();
            Assert.Contains(T.Leave, permitted); // from parent
            Assert.Contains(T.Next, permitted);  // from child
            Assert.True(m.CanFire(T.Leave));

            m.Fire(T.Leave);
            Assert.Equal(S.Outside, m.CurrentState);
        }

        [Fact]
        public void IsInHierarchy_Reports_Correctly()
        {
            var m = new InheritanceMachine(S.Outside);
            m.Start();

            m.Fire(T.Enter); // now in Parent_A
            Assert.True(m.IsInHierarchy(S.Parent));

            m.Fire(T.Leave);
            Assert.False(m.IsInHierarchy(S.Parent));
        }

#if DEBUG
        [Fact]
        public void DumpActivePath_Contains_Parent_And_Leaf()
        {
            var m = new InheritanceMachine(S.Outside);
            m.Start();
            m.Fire(T.Enter); // Parent → initial child

            var path = m.DumpActivePath();
            Assert.Contains("Parent", path);
            Assert.Contains("Parent_A", path);
        }
#endif

        public enum S { Outside, Parent, Parent_A, Parent_B }
        public enum T { Enter, Next, Leave }

        [Abstractions.Attributes.StateMachine(typeof(S), typeof(T), EnableHierarchy = true)]
        public partial class InheritanceMachine
        {
            [State(S.Parent)] private void Parent() { }
            [State(S.Parent_A, Parent = S.Parent, IsInitial = true)] private void A() { }
            [State(S.Parent_B, Parent = S.Parent)] private void B() { }

            // Parent‑level transition that applies from any child
            [Transition(S.Parent, T.Leave, S.Outside)]
            // Child‑only transition
            [Transition(S.Parent_A, T.Next, S.Parent_B)]
            // Enter composite from outside
            [Transition(S.Outside, T.Enter, S.Parent)]
            private void Configure() { }
        }
    }
    #endregion
}
