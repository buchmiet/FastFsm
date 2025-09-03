using Xunit;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version of IsInHierarchy tests
    public class HsmIsInHierarchyTests_Fluent
    {
        [Fact]
        public void IsInHierarchy_TrueForParentAndSelf_FalseForOtherBranches_Fluent()
        {
            // Use the Fluent API HSM
            var m = new SimpleParentChildMachine_Fluent(HsmState_Fluent.Idle);
            m.Start();

            // Enter composite 'Working' -> auto-jump to initial child 'Working_Initializing'
            m.Fire(HsmTrigger_Fluent.Start);

            // Self and parent are true
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Working));

            // Unrelated states are false
            Assert.False(m.IsInHierarchy(HsmState_Fluent.Completed));
            Assert.False(m.IsInHierarchy(HsmState_Fluent.Error));
            Assert.False(m.IsInHierarchy(HsmState_Fluent.Idle));
        }

        [Fact]
        public void IsInHierarchy_WorksAfterTransitionsWithinHierarchy_Fluent()
        {
            // Start in Idle, then transition to Working hierarchy
            var m = new SimpleParentChildMachine_Fluent(HsmState_Fluent.Idle);
            m.Start();
            
            // Initially in Idle
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Idle));
            Assert.False(m.IsInHierarchy(HsmState_Fluent.Working));
            
            // Transition to Working (goes to Working_Initializing)
            m.Fire(HsmTrigger_Fluent.Start);
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Working));
            Assert.False(m.IsInHierarchy(HsmState_Fluent.Idle));
            
            // Transition to Working_Processing
            m.Fire(HsmTrigger_Fluent.Process);
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Working_Processing));
            Assert.True(m.IsInHierarchy(HsmState_Fluent.Working));
            Assert.False(m.IsInHierarchy(HsmState_Fluent.Working_Initializing));
        }

        [Fact]
        public void IsInHierarchy_ReturnsFalseForInvalidStates_Fluent()
        {
            var m = new SimpleParentChildMachine_Fluent(HsmState_Fluent.Idle);
            m.Start();
            
            // Test with an invalid state value (beyond enum range)
            // This test verifies bounds checking in the implementation
            var invalidState = (HsmState_Fluent)999;
            Assert.False(m.IsInHierarchy(invalidState));
        }
    }
}