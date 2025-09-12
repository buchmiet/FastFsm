using Xunit;
using Xunit.Abstractions;
using S = FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.S;
using T = FastFsm.Tests.Features.Hsm.Runtime.SimpleParentChildMachineFluent.T;

namespace FastFsm.Tests.Features.Hsm.Runtime;

public class DebugFluentHsmTest
{
    private readonly ITestOutputHelper _output;

    public DebugFluentHsmTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_SimpleParentChildMachineFluent_BasicInfo()
    {
        var m = new SimpleParentChildMachineFluent(S.Idle);
            
        _output.WriteLine($"Initial state: {m.CurrentState}");
        _output.WriteLine($"IsStarted: {m.IsStarted}");
            
        m.Start();
        _output.WriteLine($"After Start(): {m.CurrentState}");
            
        // Check if can fire Start
        var canFire = m.CanFire(T.Start);
        _output.WriteLine($"CanFire(Start): {canFire}");
            
        // Get permitted triggers
        var permitted = m.GetPermittedTriggers();
        _output.WriteLine($"Permitted triggers: {string.Join(", ", permitted)}");
            
        // Check hierarchy info
        _output.WriteLine($"IsInHierarchy(Idle): {m.IsInHierarchy(S.Idle)}");
        _output.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(S.Working)}");
            
        if (canFire)
        {
            m.Fire(T.Start);
            _output.WriteLine($"After Fire(Start): {m.CurrentState}");
            _output.WriteLine($"CurrentState int value: {(int)m.CurrentState}");
            _output.WriteLine($"Expected Working_Initializing int: {(int)S.Working_Initializing}");
            _output.WriteLine($"IsInHierarchy(Working): {m.IsInHierarchy(S.Working)}");
            _output.WriteLine($"IsInHierarchy(Working_Initializing): {m.IsInHierarchy(S.Working_Initializing)}");
            _output.WriteLine($"IsInHierarchy(Completed): {m.IsInHierarchy(S.Completed)}");
        }
        else
        {
            _output.WriteLine("ERROR: Cannot fire Start trigger!");
        }
    }
        
}