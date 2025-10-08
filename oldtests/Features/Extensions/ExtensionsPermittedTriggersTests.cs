using System.Collections.Generic;
using FastFsm.Tests.Extensions;
using FastFsm.Tests.Machines;
using Xunit;

namespace FastFsm.Tests.Features.Extensions;

public class ExtensionsPermittedTriggersTests
{
    [Fact]
    public void GetPermittedTriggers_DoesNot_Emit_Guard_Hooks()
    {
        var log = new List<string>();
        var ext = new RecordingExtension(log);
        var machine = new Machines.HookOrderMachine(HookOrderState.A, new[] { ext });
        machine.Start();

        var permitted = machine.GetPermittedTriggers();
        Assert.Contains(HookOrderTrigger.Next, permitted);

        // No guard notifications during GetPermittedTriggers
        Assert.Empty(log);
    }

    [Fact]
    public void CanFire_DoesNot_Emit_Guard_Hooks()
    {
        var log = new List<string>();
        var ext = new RecordingExtension(log);
        var machine = new Machines.HookOrderMachine(HookOrderState.A, new[] { ext });
        machine.Start();

        var canFire = machine.CanFire(HookOrderTrigger.Next);
        Assert.True(canFire);

        // No guard notifications during CanFire
        Assert.Empty(log);
    }
}

