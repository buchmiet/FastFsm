using System;
using Xunit;

namespace FastFsm.Tests.Features.Hsm.Runtime;

public class DebugHistoryTest
{
    [Fact]
    public void Debug_ShallowHistory_Test()
    {
        var m = new ShallowHistoryTestsLegacy.ShallowHistoryMachineLegacy(ShallowHistoryTestsFluent.S.Outside);
        m.Start();
            
        Console.WriteLine($"Initial state: {m.CurrentState}");
            
        // Enter parent → initial child
        m.Fire(ShallowHistoryTestsFluent.T.Enter);
        Console.WriteLine($"After Enter: {m.CurrentState}");
            
        // Move to another child
        m.Fire(ShallowHistoryTestsFluent.T.Next);
        Console.WriteLine($"After Next: {m.CurrentState}");
            
        // Exit composite
        m.Fire(ShallowHistoryTestsFluent.T.Exit);
        Console.WriteLine($"After Exit: {m.CurrentState}");
            
        // Re‑enter → shallow history brings us back to Settings
        m.Fire(ShallowHistoryTestsFluent.T.Enter);
        Console.WriteLine($"After re-Enter: {m.CurrentState}");
            
        // Expected: Menu_Settings, Actual: ?
        Assert.Equal(ShallowHistoryTestsFluent.S.Menu_Settings, m.CurrentState);
    }
}