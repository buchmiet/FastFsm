using Xunit;
using StateMachine.Tests.HierarchicalTests;

namespace StateMachine.Tests.HierarchicalRuntime
{
    public class HsmIsInHierarchyTests
    {
        [Fact]
        public void IsInHierarchy_TrueForParentAndSelf_FalseForOtherBranches()
        {
            // Use the validated HSM from HsmParsingCompilationTests
            var m = new HsmParsingCompilationTests.SimpleParentChildMachine(HsmParsingCompilationTests.HsmState.Idle);
            m.Start();

            // Enter composite 'Working' -> auto-jump to initial child 'Working_Initializing'
            m.Fire(HsmParsingCompilationTests.HsmTrigger.Start);

            // Self and parent are true
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working));

            // Unrelated states are false
            Assert.False(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Completed));
            Assert.False(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Error));
            Assert.False(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Idle));
        }

        [Fact]
        public void IsInHierarchy_WorksAfterTransitionsWithinHierarchy()
        {
            // Start in Idle, then transition to Working hierarchy
            var m = new HsmParsingCompilationTests.SimpleParentChildMachine(HsmParsingCompilationTests.HsmState.Idle);
            m.Start();
            
            // Initially in Idle
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Idle));
            Assert.False(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working));
            
            // Transition to Working (goes to Working_Initializing)
            m.Fire(HsmParsingCompilationTests.HsmTrigger.Start);
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working_Initializing));
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working));
            Assert.False(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Idle));
            
            // Transition to Working_Processing
            m.Fire(HsmParsingCompilationTests.HsmTrigger.Process);
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working_Processing));
            Assert.True(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working));
            Assert.False(m.IsInHierarchy(HsmParsingCompilationTests.HsmState.Working_Initializing));
        }

        [Fact]
        public void IsInHierarchy_ReturnsFalseForInvalidStates()
        {
            var m = new HsmParsingCompilationTests.SimpleParentChildMachine(HsmParsingCompilationTests.HsmState.Idle);
            m.Start();
            
            // Test with an invalid state value (beyond enum range)
            // This test verifies bounds checking in the implementation
            var invalidState = (HsmParsingCompilationTests.HsmState)999;
            Assert.False(m.IsInHierarchy(invalidState));
        }
    }
}