using System;
using Xunit;
using FastFsm.Tests.Features.Hsm.Runtime;

namespace FastFsm.Tests.Features.Hsm.Runtime
{
    public class DebugHistoryTest
    {
        [Fact]
        public void Debug_ShallowHistory_Test()
        {
            var m = new ShallowHistoryTests.ShallowHistoryMachine(ShallowHistoryTests.S.Outside);
            m.Start();
            
            Console.WriteLine($"Initial state: {m.CurrentState}");
            
            // Enter parent → initial child
            m.Fire(ShallowHistoryTests.T.Enter);
            Console.WriteLine($"After Enter: {m.CurrentState}");
            
            // Move to another child
            m.Fire(ShallowHistoryTests.T.Next);
            Console.WriteLine($"After Next: {m.CurrentState}");
            
            // Exit composite
            m.Fire(ShallowHistoryTests.T.Exit);
            Console.WriteLine($"After Exit: {m.CurrentState}");
            
            // Re‑enter → shallow history brings us back to Settings
            m.Fire(ShallowHistoryTests.T.Enter);
            Console.WriteLine($"After re-Enter: {m.CurrentState}");
            
            // Expected: Menu_Settings, Actual: ?
            Assert.Equal(ShallowHistoryTests.S.Menu_Settings, m.CurrentState);
        }
    }
}
