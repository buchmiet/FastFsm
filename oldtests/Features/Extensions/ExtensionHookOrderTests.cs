using System.Collections.Generic;
using Abstractions.Attributes;
using FastFsm.Tests.Extensions;
using Xunit;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Features.Extensions
{

    public class ExtensionHookOrderTests
    {
        [Fact]
        public void Hooks_AreInvoked_InExpectedOrder()
        {
            // arrange
            var log = new List<string>();
            var ext = new RecordingExtension(log);
            var machine = new Machines.HookOrderMachine(HookOrderState.A, [ext]);
            machine.Start();

            // act
            machine.TryFire(HookOrderTrigger.Next);

            // assert – pełna sekwencja
            var expected = new[]
            {
                "Before",
                "GuardEval",
                "GuardEvaluated",
                "After:Success"
            };
            Assert.Equal(expected, log);
        }

    }
}
