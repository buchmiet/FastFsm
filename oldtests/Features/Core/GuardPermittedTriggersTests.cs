using Abstractions.Attributes;
using Xunit;
using FastFsm.Tests.Machines;

namespace FastFsm.Tests.Features.Core
{
    public class GuardPermittedTriggersTests
    {
        [Fact]
        public void PermittedTriggers_ReflectCurrentGuardState()
        {
            var machine = new Machines.GuardPermittedMachine(GuardPermittedState.Idle)
            {
                // guard initially false
                Allow = false
            };
            machine.Start();

            Assert.DoesNotContain(GuardPermittedTrigger.Run, machine.GetPermittedTriggers());

            // guard true
            machine.Allow = true;
            Assert.Contains(GuardPermittedTrigger.Run, machine.GetPermittedTriggers());
        }

    }


}
