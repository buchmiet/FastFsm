using Xunit;
using Abstractions.Attributes;
using FastFsm.Tests.Machines;

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
[Fact]
        public void EqualPriorityMachine_Compiles()
        {
            var m = new EqualPriorityMachine(EP_State.A);
            Assert.NotNull(m);
        }

        // ==========================================
        // 3) EnableHierarchy=true but no hierarchy
        // ==========================================
[Fact]
        public void HierarchyFlagNoHierarchyMachine_Compiles()
        {
            var m = new HierarchyFlagNoHierarchyMachine(NH_State.S1);
            Assert.NotNull(m);
        }
    }
}
