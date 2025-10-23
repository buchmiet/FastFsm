using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;

namespace FastFsm.Tests.Hsm.Runtime
{
    public class HsmIsInHierarchyTests
    {
        [Fact]
        public void IsInHierarchy_TrueForParentAndSelf_FalseForOtherBranches()
        {
            // Use the validated HSM from HsmParsingCompilationTests
            var m = new SimpleParentChildMachine(HsmState.Idle);
            m.Start();

            // Enter composite 'Working' -> auto-jump to initial child 'Working_Initializing'
            m.Fire( HsmTrigger.Start);

            // Self and parent are true
            Assert.True(m.IsInHierarchy( HsmState.Working_Initializing));
            Assert.True(m.IsInHierarchy( HsmState.Working));

            // Unrelated states are false
            Assert.False(m.IsInHierarchy( HsmState.Completed));
            Assert.False(m.IsInHierarchy( HsmState.Error));
            Assert.False(m.IsInHierarchy( HsmState.Idle));
        }

        [Fact]
        public void IsInHierarchy_WorksAfterTransitionsWithinHierarchy()
        {
            // Start in Idle, then transition to Working hierarchy
            var m = new  SimpleParentChildMachine( HsmState.Idle);
            m.Start();

            // Initially in Idle
            Assert.True(m.IsInHierarchy( HsmState.Idle));
            Assert.False(m.IsInHierarchy( HsmState.Working));

            // Transition to Working (goes to Working_Initializing)
            m.Fire( HsmTrigger.Start);
            Assert.True(m.IsInHierarchy( HsmState.Working_Initializing));
            Assert.True(m.IsInHierarchy( HsmState.Working));
            Assert.False(m.IsInHierarchy( HsmState.Idle));

            // Transition to Working_Processing
            m.Fire( HsmTrigger.Process);
            Assert.True(m.IsInHierarchy( HsmState.Working_Processing));
            Assert.True(m.IsInHierarchy( HsmState.Working));
            Assert.False(m.IsInHierarchy( HsmState.Working_Initializing));
        }

        [Fact]
        public void IsInHierarchy_ReturnsFalseForInvalidStates()
        {
            var m = new  SimpleParentChildMachine( HsmState.Idle);
            m.Start();

            // Test with an invalid state value (beyond enum range)
            // This test verifies bounds checking in the implementation
            var invalidState = ( HsmState)999;
            Assert.False(m.IsInHierarchy(invalidState));
        }
    }
}
