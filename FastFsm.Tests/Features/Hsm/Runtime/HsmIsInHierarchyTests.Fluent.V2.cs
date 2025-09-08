using Xunit;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    // Tests for Fluent API version using Abstractions.Fluent
    public class HsmIsInHierarchyTestsFluentV2
    {
        [Fact]
        public void IsInHierarchy_TrueForParentAndSelf_FalseForOtherBranchesFluentV2()
        {
            // Use the Fluent API HSM
            var m = new SimpleParentChildMachineFluent_v2(HsmStateFluent_v2.Idle);
            m.Start();

            // Enter composite 'Working' -> auto-jump to initial child 'Working_Initializing'
            m.Fire(HsmTriggerFluent_v2.Start);

            // Self and parent are true
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Working));

            // Unrelated states are false
            Assert.False(m.IsInHierarchy(HsmStateFluent_v2.Completed));
            Assert.False(m.IsInHierarchy(HsmStateFluent_v2.Error));
            Assert.False(m.IsInHierarchy(HsmStateFluent_v2.Idle));
        }

        [Fact]
        public void IsInHierarchy_WorksAfterTransitionsWithinHierarchyFluentV2()
        {
            // Start in Idle, then transition to Working hierarchy
            var m = new SimpleParentChildMachineFluent_v2(HsmStateFluent_v2.Idle);
            m.Start();
            
            // Initially in Idle
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Idle));
            Assert.False(m.IsInHierarchy(HsmStateFluent_v2.Working));
            
            // Transition to Working (goes to Working_Initializing)
            m.Fire(HsmTriggerFluent_v2.Start);
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Working));
            Assert.False(m.IsInHierarchy(HsmStateFluent_v2.Idle));
            
            // Transition to Working_Processing
            m.Fire(HsmTriggerFluent_v2.Process);
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Working_Processing));
            Assert.True(m.IsInHierarchy(HsmStateFluent_v2.Working));
            Assert.False(m.IsInHierarchy(HsmStateFluent_v2.Working_Initializing));
        }

        [Fact]
        public void IsInHierarchy_ReturnsFalseForInvalidStatesFluentV2()
        {
            var m = new SimpleParentChildMachineFluent_v2(HsmStateFluent_v2.Idle);
            m.Start();
            
            // Test with an invalid state value (beyond enum range)
            // This test verifies bounds checking in the implementation
            var invalidState = (HsmStateFluent_v2)999;
            Assert.False(m.IsInHierarchy(invalidState));
        }
        
        [Fact]
        public void FluentV2_BasicTransitions_Work()
        {
            var m = new SimpleParentChildMachineFluent_v2(HsmStateFluent_v2.Idle);
            m.Start();
            Assert.Equal(HsmStateFluent_v2.Idle, m.CurrentState);
            
            // Should transition from Idle to Working (which auto-enters initial child Working_Initializing)
            m.Fire(HsmTriggerFluent_v2.Start);
            Assert.Equal(HsmStateFluent_v2.Working_Initializing, m.CurrentState);
        }
    }
}