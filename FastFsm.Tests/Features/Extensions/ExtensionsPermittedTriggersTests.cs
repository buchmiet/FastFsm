using System.Collections.Generic;
using Xunit;

namespace StateMachine.Tests.Features.Extensions;

public class ExtensionsPermittedTriggersTests
{
    [Fact]
    public void GetPermittedTriggers_DoesNot_Emit_Guard_Hooks()
    {
        var log = new List<string>();
        var ext = new RecordingExtension(log);
        var machine = new HookOrderMachine(State.A, new[] { ext });
        machine.Start();

        var permitted = machine.GetPermittedTriggers();
        Assert.Contains(Trigger.Next, permitted);

        // No guard notifications during GetPermittedTriggers
        Assert.Empty(log);
    }

    [Fact]
    public void CanFire_DoesNot_Emit_Guard_Hooks()
    {
        var log = new List<string>();
        var ext = new RecordingExtension(log);
        var machine = new HookOrderMachine(State.A, new[] { ext });
        machine.Start();

        var canFire = machine.CanFire(Trigger.Next);
        Assert.True(canFire);

        // No guard notifications during CanFire
        Assert.Empty(log);
    }
}

