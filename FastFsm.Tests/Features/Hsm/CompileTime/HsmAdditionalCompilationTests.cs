using Xunit;
using Abstractions.Attributes;

namespace FastFsm.Tests.Features.Hsm.CompileTime
{
    /// <summary>
    /// Additional compilation-only tests to ensure the generator accepts
    /// specific valid configurations described in feature 0.7.
    /// </summary>
    public partial class HsmAdditionalCompilationTests
    {
        // =========================
        // 1) HSM + Payload variant
        // =========================
        public enum HP_State { Root, ChildA, ChildB }
        public enum HP_Trigger { Configure, Submit }

        [StateMachine(typeof(HP_State), typeof(HP_Trigger), EnableHierarchy = true)]
        [PayloadType(typeof(PayloadData))]
        public partial class HsmPayloadMachine
        {
            [State(HP_State.Root)] private void S_Root() { }
            [State(HP_State.ChildA, Parent = HP_State.Root, IsInitial = true)] private void S_ChildA() { }
            [State(HP_State.ChildB, Parent = HP_State.Root)] private void S_ChildB() { }

            // Internal (no state change) with payload
            [InternalTransition(HP_State.ChildA, HP_Trigger.Configure, Action = nameof(ConfigureAction))]
            // External with payload (guard + action)
            [Transition(HP_State.ChildA, HP_Trigger.Submit, HP_State.ChildB, Guard = nameof(CanSubmit), Action = nameof(SubmitAction))]
            private void T_All() { }

            private void ConfigureAction(PayloadData p) { }
            private void SubmitAction(PayloadData p) { }
            private bool CanSubmit(PayloadData p) => true;
        }

        [Fact]
        public void HsmPayloadMachine_CompilesAndBasicUsageCompiles()
        {
            var m = new HsmPayloadMachine(HP_State.Root);
            Assert.NotNull(m);
            Assert.Equal(HP_State.Root, m.CurrentState); // Before Start, should be at Root
            
            // Basic API usage with payload (compile-time check)
            var payload = new PayloadData { Value = 42 };
            m.Start();
            Assert.Equal(HP_State.ChildA, m.CurrentState); // After Start, should descend to initial child
            
            m.Fire(HP_Trigger.Configure, payload); // internal branch
            m.TryFire(HP_Trigger.Submit, payload); // external branch
        }

        public class PayloadData { public int Value { get; set; } }

        // =========================
        // 2) Equal priority transitions
        // =========================
        public enum EP_State { A, B, C }
        public enum EP_Trigger { Go }

        [StateMachine(typeof(EP_State), typeof(EP_Trigger), EnableHierarchy = true)]
        public partial class EqualPriorityMachine
        {
            // Two transitions with the same priority from the same state/trigger.
            // Should compile; generator may warn but must not fail.
            [Transition(EP_State.A, EP_Trigger.Go, EP_State.B, Priority = 100, Action = nameof(ActionB))]
            [Transition(EP_State.A, EP_Trigger.Go, EP_State.C, Priority = 100, Action = nameof(ActionC))]
            private void T_Priority() { }

            private void ActionB() { }
            private void ActionC() { }
        }

        [Fact]
        public void EqualPriorityMachine_Compiles()
        {
            var m = new EqualPriorityMachine(EP_State.A);
            Assert.NotNull(m);
        }

        // ==========================================
        // 3) EnableHierarchy=true but no hierarchy
        // ==========================================
        public enum NH_State { S1, S2 }
        public enum NH_Trigger { Next }

        [StateMachine(typeof(NH_State), typeof(NH_Trigger), EnableHierarchy = true)]
        public partial class HierarchyFlagNoHierarchyMachine
        {
            // Flat states (no Parent/IsInitial/History)
            [State(NH_State.S1)] private void S_S1() { }
            [State(NH_State.S2)] private void S_S2() { }

            [Transition(NH_State.S1, NH_Trigger.Next, NH_State.S2)]
            private void T_Flat() { }
        }

        [Fact]
        public void HierarchyFlagNoHierarchyMachine_Compiles()
        {
            var m = new HierarchyFlagNoHierarchyMachine(NH_State.S1);
            Assert.NotNull(m);
        }
    }
}
