using System.Collections.Generic;
using Machines.Tests.Extensions;
using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;

namespace FastFsm.Tests.Extensions
{

    public class ExtensionHookOrderTests
    {
        [Fact]
        public void Hooks_AreInvoked_InExpectedOrder()
        {
            // arrange
            var log = new List<string>();
            var ext = new RecordingExtension(log);
            var machine = new HookOrderMachine(HookOrderState.A, [ext]);
            machine.Start();

            // act
            machine.TryFire(HookOrderTrigger.Next);

            // assert – pełna sekwencja
            var expected = new[]
            {
                "Before",
                "GuardEval",
                "GuardEvaluated",
                "Transitioned",
                "After:Success"
            };
            Assert.Equal(expected, log);
        }

    }
}
