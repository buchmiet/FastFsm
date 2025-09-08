using Xunit;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Fluent API version of IsInHierarchy tests
    public class HsmIsInHierarchyTestsFluent
    {
        [Fact]
        public void IsInHierarchy_TrueForParentAndSelf_FalseForOtherBranchesFluent()
        {
            // Use the Fluent API HSM
            var m = new SimpleParentChildMachineFluent(HsmStateFluent.Idle);
            m.Start();

            // Enter composite 'Working' -> auto-jump to initial child 'Working_Initializing'
            m.Fire(HsmTriggerFluent.Start);

            // Self and parent are true
            Assert.True(m.IsInHierarchy(HsmStateFluent.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmStateFluent.Working));

            // Unrelated states are false
            Assert.False(m.IsInHierarchy(HsmStateFluent.Completed));
            Assert.False(m.IsInHierarchy(HsmStateFluent.Error));
            Assert.False(m.IsInHierarchy(HsmStateFluent.Idle));
        }

        [Fact]
        public void IsInHierarchy_WorksAfterTransitionsWithinHierarchyFluent()
        {
            // Start in Idle, then transition to Working hierarchy
            var m = new SimpleParentChildMachineFluent(HsmStateFluent.Idle);
            m.Start();
            
            // Initially in Idle
            Assert.True(m.IsInHierarchy(HsmStateFluent.Idle));
            Assert.False(m.IsInHierarchy(HsmStateFluent.Working));
            
            // Transition to Working (goes to Working_Initializing)
            m.Fire(HsmTriggerFluent.Start);
            Assert.True(m.IsInHierarchy(HsmStateFluent.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmStateFluent.Working));
            Assert.False(m.IsInHierarchy(HsmStateFluent.Idle));
            
            // Transition to Working_Processing
            m.Fire(HsmTriggerFluent.Process);
            Assert.True(m.IsInHierarchy(HsmStateFluent.Working_Processing));
            Assert.True(m.IsInHierarchy(HsmStateFluent.Working));
            Assert.False(m.IsInHierarchy(HsmStateFluent.Working_Initializing));
        }

        [Fact]
        public void IsInHierarchy_ReturnsFalseForInvalidStatesFluent()
        {
            var m = new SimpleParentChildMachineFluent(HsmStateFluent.Idle);
            m.Start();
            
            // Test with an invalid state value (beyond enum range)
            // This test verifies bounds checking in the implementation
            var invalidState = (HsmStateFluent)999;
            Assert.False(m.IsInHierarchy(invalidState));
        }
    }
}