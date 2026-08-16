using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace FastFsm.Instance.Tests;

public class InstantiatedMachinesTests
{
    [Fact]
    public void GuardMachine_AllowsAndBlocksBasedOnInstanceState()
    {
        var machine = new GuardInstanceMachine(GuardInstanceMachine.State.Idle);
        machine.Start();

        machine.SetAllowStart(false);
        machine.TryFire(GuardInstanceMachine.Trigger.Start).ShouldBeFalse();
        machine.CurrentState.ShouldBe(GuardInstanceMachine.State.Idle);

        machine.SetAllowStart(true);
        machine.TryFire(GuardInstanceMachine.Trigger.Start).ShouldBeTrue();
        machine.CurrentState.ShouldBe(GuardInstanceMachine.State.Active);
    }

    [Fact]
    public void PayloadMachine_UsesMethodGroupCallbacksWithPayload()
    {
        var machine = new PayloadInstanceMachine(PayloadInstanceMachine.State.Idle);
        machine.Start();

        var invalid = new PayloadInstanceMachine.OrderPayload(string.Empty);
        machine.TryFire(PayloadInstanceMachine.Trigger.Submit, invalid).ShouldBeFalse();
        machine.CurrentState.ShouldBe(PayloadInstanceMachine.State.Idle);
        machine.LastPayload.ShouldBeNull();

        var valid = new PayloadInstanceMachine.OrderPayload("order-42");
        machine.TryFire(PayloadInstanceMachine.Trigger.Submit, valid).ShouldBeTrue();
        machine.CurrentState.ShouldBe(PayloadInstanceMachine.State.Processing);
        machine.LastPayload.ShouldBe(valid);

        machine.TryFire(PayloadInstanceMachine.Trigger.Finish).ShouldBeTrue();
        machine.CurrentState.ShouldBe(PayloadInstanceMachine.State.Completed);
        machine.LastPayload.ShouldBeNull();
    }

    [Fact]
    public async Task AsyncMachine_ExecutesAsyncGuardsAndActions()
    {
        var machine = new AsyncInstanceMachine(AsyncInstanceMachine.State.Idle);
        await machine.StartAsync();

        (await machine.TryFireAsync(AsyncInstanceMachine.Trigger.Activate)).ShouldBeTrue();
        machine.CurrentState.ShouldBe(AsyncInstanceMachine.State.Busy);
        machine.Events.ShouldContain("begin");

        (await machine.TryFireAsync(AsyncInstanceMachine.Trigger.Deactivate)).ShouldBeTrue();
        machine.CurrentState.ShouldBe(AsyncInstanceMachine.State.Idle);
        machine.Events.ShouldBe(new[] { "begin", "close" });
    }
}
