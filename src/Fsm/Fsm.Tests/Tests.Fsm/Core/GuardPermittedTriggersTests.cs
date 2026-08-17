using Abstractions.Attributes;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;


namespace Tests.Fsm.Core
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
