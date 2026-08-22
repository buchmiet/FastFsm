using System.Collections.Generic;
using Tests.Machines.Extensions;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Extensions
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

            // assert – full sequence
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
