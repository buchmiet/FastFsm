using Machines.Tests.Features.Core;
using Machines.Tests.Machines;
using Xunit;

namespace FastFsm.Tests.Features.Core;

public class GuardPermittedTriggersTests
{
    [Fact]
    public void PermittedTriggers_ReflectCurrentGuardState()
    {
        var machine = new GuardPermittedMachineLegacy(State.Idle)
        {
            Allow = false
        };

        machine.Start();

        Assert.DoesNotContain(Trigger.Run, machine.GetPermittedTriggers());

        machine.Allow = true;
        Assert.Contains(Trigger.Run, machine.GetPermittedTriggers());
    }
}
