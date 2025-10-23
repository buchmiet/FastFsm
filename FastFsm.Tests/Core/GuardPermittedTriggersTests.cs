using Abstractions.Attributes;
using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;


namespace FastFsm.Tests.Core
{
    public class GuardPermittedTriggersTests
    {
        [Fact]
        public void PermittedTriggers_ReflectCurrentGuardState()
        {
            var machine = new GuardPermittedMachine(GuardPermittedState.Idle)
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
