using Xunit;
using S = FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S;
using T = FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T;

namespace FastFsm.Tests.Features.Hsm.Runtime;

// Legacy API version of IsInHierarchy tests
public class HsmIsInHierarchyTestsLegacy
{
    [Fact]
    public void IsInHierarchy_TrueForParentAndSelf_FalseForOtherBranchesLegacy()
    {
        // Use the Legacy API HSM
        var m = new SimpleParentChildMachineLegacy(S.Idle);
        m.Start();

        // Enter composite 'Working' -> auto-jump to initial child 'Working_Initializing'
        m.Fire(T.Start);

        // Self and parent are true
        Assert.True(m.IsInHierarchy(S.Working_Initializing));
        Assert.True(m.IsInHierarchy(S.Working));

        // Unrelated states are false
        Assert.False(m.IsInHierarchy(S.Completed));
        Assert.False(m.IsInHierarchy(S.Error));
        Assert.False(m.IsInHierarchy(S.Idle));
    }

    [Fact]
    public void IsInHierarchy_WorksAfterTransitionsWithinHierarchyLegacy()
    {
        // Start in Idle, then transition to Working hierarchy
        var m = new SimpleParentChildMachineLegacy(S.Idle);
        m.Start();
            
        // Initially in Idle
        Assert.True(m.IsInHierarchy(S.Idle));
        Assert.False(m.IsInHierarchy(S.Working));
            
        // Transition to Working (goes to Working_Initializing)
        m.Fire(T.Start);
        Assert.True(m.IsInHierarchy(S.Working_Initializing));
        Assert.True(m.IsInHierarchy(S.Working));
        Assert.False(m.IsInHierarchy(S.Idle));
            
        // Transition to Working_Processing
        m.Fire(T.Process);
        Assert.True(m.IsInHierarchy(S.Working_Processing));
        Assert.True(m.IsInHierarchy(S.Working));
        Assert.False(m.IsInHierarchy(S.Working_Initializing));
    }

    [Fact]
    public void IsInHierarchy_ReturnsFalseForInvalidStatesLegacy()
    {
        var m = new SimpleParentChildMachineLegacy(S.Idle);
        m.Start();
            
        // Test with an invalid state value (beyond enum range)
        // This test verifies bounds checking in the implementation
        var invalidState = (S)999;
        Assert.False(m.IsInHierarchy(invalidState));
    }
}