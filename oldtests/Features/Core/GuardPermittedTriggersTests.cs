using Abstractions.Attributes;
using FastFsm.Tests.Machines;
using Xunit;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Features.Core
{
    public class GuardPermittedTriggersTests
    {
        [Fact]
        public void PermittedTriggers_ReflectCurrentGuardState()
        {
            var machine = new Machines.Legacy.GuardPermittedMachine(GuardPermittedState.Idle)
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
