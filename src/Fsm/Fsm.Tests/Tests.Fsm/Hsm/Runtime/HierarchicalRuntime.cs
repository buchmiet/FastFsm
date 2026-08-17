using System;
using System.Reflection;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Hsm.Runtime
{
    #region 1) Auto‑descend to initial child + basic parent/child wiring
    public partial class InitialChildTests
    {
        [Fact]
        public void Transition_ToCompositeParent_Enters_ItsInitialChild()
        {
            var m = new InitialChildMachine(InitialChildMachine_S.Outside);
            m.Start();

            Assert.Equal(InitialChildMachine_S.Outside, m.CurrentState);

            m.Fire(InitialChildMachine_T.EnterParent);
            Assert.Equal(InitialChildMachine_S.Parent_A, m.CurrentState); // auto‑descend to initial child

            m.Fire(InitialChildMachine_T.Switch);
            Assert.Equal(InitialChildMachine_S.Parent_B, m.CurrentState);

            m.Fire(InitialChildMachine_T.LeaveParent);
            Assert.Equal(InitialChildMachine_S.Outside, m.CurrentState);
        }
    }
    #endregion

    #region 2) Shallow history remembers last child
    public partial class ShallowHistoryTests
    {
        [Fact]
        public void Reentering_Parent_With_ShallowHistory_Restores_LastChild()
        {
            var m = new ShallowHistoryMachine(ShallowHistoryMachine_S.Outside);
            m.Start();

            // Enter parent → initial child
            m.Fire(ShallowHistoryMachine_T.Enter);
            Assert.Equal(ShallowHistoryMachine_S.Menu_Main, m.CurrentState);

            // Move to another child
            m.Fire(ShallowHistoryMachine_T.Next);
            Assert.Equal(ShallowHistoryMachine_S.Menu_Settings, m.CurrentState);

            // Exit composite
            m.Fire(ShallowHistoryMachine_T.Exit);
            Assert.Equal(ShallowHistoryMachine_S.Outside, m.CurrentState);

            // Re‑enter → shallow history brings us back to Settings
            m.Fire(ShallowHistoryMachine_T.Enter);
            Assert.Equal(ShallowHistoryMachine_S.Menu_Settings, m.CurrentState);
        }
    }
    #endregion

    #region 3) Deep history restores entire path
    public partial class DeepHistoryTests
    {
        [Fact]
        public void DeepHistory_Arrays_Generated_Correctly()
        {
            var type = typeof(DeepHistoryMachine);

            // instancja jest potrzebna, jeśli będziemy czytać chronione właściwości
            var instance = Activator.CreateInstance(type, DeepHistoryMachine_S.Out)!;

            // spróbuj: g_*  -> s_* (wsteczna zgodność) -> chronione właściwości z instancji
            int[] parent = GetIntArray(type, instance, "g_parent", "ParentArray");
            int[] initial = GetIntArray(type, instance, "g_initialChild", "InitialChildArray");
            int[] depth = GetIntArray(type, instance, "g_depth", "DepthArray");
            Array history = GetArray(type, instance, "g_history", "HistoryArray");

            // Expected: Out=0, Work=1, Work_S1=2, Work_S1_Loading=3, Work_S1_Calc=4
            Assert.Equal(new[] { -1, -1, 1, 2, 2 }, parent);
            Assert.Equal(new[] { -1, 2, 3, -1, -1 }, initial);
            Assert.Equal(new[] { 0, 0, 1, 2, 2 }, depth);

            // Only Work has Deep history
            for (int i = 0; i < history.Length; i++)
                Console.WriteLine($"history[{i}] = {history.GetValue(i)}");

            Assert.Equal("None", history.GetValue(0)!.ToString());
            Assert.Equal("Deep", history.GetValue(1)!.ToString());
            Assert.Equal("None", history.GetValue(2)!.ToString());
            Assert.Equal("None", history.GetValue(3)!.ToString());
            Assert.Equal("None", history.GetValue(4)!.ToString());
        }

        // --- helpers ---

        static int[] GetIntArray(Type t, object instance, string staticFieldName, string protectedPropName)
        {
            // 1) nowe pole g_*
            var f = t.GetField(staticFieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null) return (int[])f.GetValue(null)!;

            // 2) wsteczna zgodność: stare pole s_*
            var legacy = staticFieldName.Replace("g_", "s_");
            f = t.GetField(legacy, BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null) return (int[])f.GetValue(null)!;

            // 3) chroniona właściwość bazowa (override w klasie wygenerowanej)
            var p = t.GetProperty(protectedPropName, BindingFlags.NonPublic | BindingFlags.Instance)
                 ?? t.BaseType?.GetProperty(protectedPropName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return (int[])p.GetValue(instance)!;

            throw new InvalidOperationException($"Nie znaleziono {staticFieldName}/{protectedPropName} w {t.FullName}.");
        }

        static Array GetArray(Type t, object instance, string staticFieldName, string protectedPropName)
        {
            // 1) nowe pole g_*
            var f = t.GetField(staticFieldName, BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null) return (Array)f.GetValue(null)!;

            // 2) wsteczna zgodność: stare pole s_*
            var legacy = staticFieldName.Replace("g_", "s_");
            f = t.GetField(legacy, BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null) return (Array)f.GetValue(null)!;

            // 3) chroniona właściwość bazowa
            var p = t.GetProperty(protectedPropName, BindingFlags.NonPublic | BindingFlags.Instance)
                 ?? t.BaseType?.GetProperty(protectedPropName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return (Array)p.GetValue(instance)!;

            throw new InvalidOperationException($"Nie znaleziono {staticFieldName}/{protectedPropName} w {t.FullName}.");
        }

        [Fact]
        public void DeepHistory_Restores_LeafPath_After_Reentering()
        {
            var m = new DeepHistoryMachine(DeepHistoryMachine_S.Out);
            m.Start();

            // Enter composite → auto path: Work → S1 (initial) → Loading (initial)
            m.Fire(DeepHistoryMachine_T.EnterWork);
            Assert.Equal(DeepHistoryMachine_S.Work_S1_Loading, m.CurrentState);

            // Move to deeper sibling leaf
            m.Fire(DeepHistoryMachine_T.Next);
            Assert.Equal(DeepHistoryMachine_S.Work_S1_Calc, m.CurrentState);

            // Exit composite to outside
            m.Fire(DeepHistoryMachine_T.Abort);
            Assert.Equal(DeepHistoryMachine_S.Out, m.CurrentState);

            // Re‑enter → deep history returns to the last leaf (Calc)
            m.Fire(DeepHistoryMachine_T.EnterWork);
            Assert.Equal(DeepHistoryMachine_S.Work_S1_Calc, m.CurrentState);
        }
    }
    #endregion

    #region 4) Internal transitions: no state change and no entry/exit
    public partial class InternalTransitionTests
    {
        [Fact]
        public void Internal_OnParent_Executes_Action_Without_ExitOrEntry()
        {
            var m = new InternalMachine(InternalMachine_S.Parent);
            m.Start(); // auto enters Child
            m.Log.Clear();

            m.Fire(InternalMachine_T.Refresh);

            Assert.Equal(InternalMachine_S.Child, m.CurrentState); // state unchanged
            Assert.Equal(new[] { "ParentInternal" }, m.Log);
        }

        [Fact]
        public void Internal_OnChild_Overrides_Parent_When_PriorityEqual()
        {
            var m = new InternalMachine(InternalMachine_S.Parent) { UseChildInternal = true };
            m.Start();
            m.Log.Clear();

            m.Fire(InternalMachine_T.Refresh);

            Assert.Equal(InternalMachine_S.Child, m.CurrentState);
            Assert.Equal(new[] { "ChildInternal" }, m.Log);
        }
    }
    #endregion

    #region 5) Resolution order: Priority → Child over Parent → Source order
    public partial class ResolutionOrderTests
    {
        [Fact]
        public void HigherPriority_Wins_Even_If_Parent()
        {
            var m = new PriorityMachine(PriorityMachine_S.Parent);
            m.Start(); // enters Child
            m.Fire(PriorityMachine_T.Go);
            Assert.Equal(PriorityMachine_S.ParentDone, m.CurrentState); // parent wins due to higher priority
            Assert.Equal(new[] { "Parent" }, m.Log);
        }

        [Fact]
        public void ChildOverridesParent_When_PriorityEqual()
        {
            var m = new ChildOverridesMachine(ChildOverridesMachine_S.Parent);
            m.Start();
            m.Fire(ChildOverridesMachine_T.Go);
            Assert.Equal(ChildOverridesMachine_S.Child, m.CurrentState); // self‑transition on child
            Assert.Equal(new[] { "Child" }, m.Log);
        }

        [Fact]
        public void SourceOrder_Breaks_Ties_Within_Same_State()
        {
            var m = new SourceOrderTieMachine(SourceOrderTieMachine_S.A);
            m.Start();
            m.Fire(SourceOrderTieMachine_T.Go);
            Assert.Equal(SourceOrderTieMachine_S.B, m.CurrentState); // first declared wins
            Assert.Equal(new[] { "First" }, m.Log);
        }
    }
    #endregion

    #region 6) Inheritance + GetPermittedTriggers/CanFire + IsInHierarchy + DumpActivePath
    public partial class InheritanceAndIntrospectionTests
    {
        [Fact]
        public void Child_Inherits_Parent_Transitions_And_PermittedTriggers_Unions()
        {
            var m = new InheritanceMachine(InheritanceMachine_S.Outside);
            m.Start();

            // Enter the composite parent
            m.Fire(InheritanceMachine_T.Enter);
            Assert.Equal(InheritanceMachine_S.Parent_A, m.CurrentState);

            var permitted = m.GetPermittedTriggers();
            Assert.Contains(InheritanceMachine_T.Leave, permitted); // from parent
            Assert.Contains(InheritanceMachine_T.Next, permitted);  // from child
            Assert.True(m.CanFire(InheritanceMachine_T.Leave));

            m.Fire(InheritanceMachine_T.Leave);
            Assert.Equal(InheritanceMachine_S.Outside, m.CurrentState);
        }

        [Fact]
        public void IsInHierarchy_Reports_Correctly()
        {
            var m = new InheritanceMachine(InheritanceMachine_S.Outside);
            m.Start();

            m.Fire(InheritanceMachine_T.Enter); // now in Parent_A
            Assert.True(m.IsInHierarchy(InheritanceMachine_S.Parent));

            m.Fire(InheritanceMachine_T.Leave);
            Assert.False(m.IsInHierarchy(InheritanceMachine_S.Parent));
        }

#if DEBUG
        [Fact]
        public void DumpActivePath_Contains_Parent_And_Leaf()
        {
            var m = new InheritanceMachine(InheritanceMachine_S.Outside);
            m.Start();
            m.Fire(InheritanceMachine_T.Enter); // Parent → initial child

            var path = m.DumpActivePath();
            Assert.Contains("Parent", path);
            Assert.Contains("Parent_A", path);
        }
#endif
    }
    #endregion
}
