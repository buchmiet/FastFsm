using Example.Tests.Unreachable;
using Example.Tests.Unreachable.Legacy;
using Example.Tests.Unreachable.Fluent;

namespace Example.Tests.UnreachableTests;

public class UnreachableMachineTests
{
    [Fact]
    public void StateMachine_WithUnreachableStates_ShouldStillFunction()
    {
        var machine = new UnreachableMachineLegacy(UnreachableState.Start);
        machine.Start();

        Assert.Equal(UnreachableState.Start, machine.CurrentState);
        Assert.Single(machine.GetPermittedTriggers());

        Assert.True(machine.TryFire(UnreachableTrigger.Connect));
        Assert.Equal(UnreachableState.Connected, machine.CurrentState);

        Assert.False(machine.CanFire(UnreachableTrigger.Isolate));
        var permittedFromConnected = machine.GetPermittedTriggers();
        Assert.DoesNotContain(UnreachableTrigger.Isolate, permittedFromConnected);
    }

    [Fact]
    public void Fluent_StateMachine_WithUnreachableStates_ShouldStillFunction()
    {
        var machine = new UnreachableMachine(UnreachableState.Start);
        machine.Start();

        Assert.Equal(UnreachableState.Start, machine.CurrentState);
        Assert.Single(machine.GetPermittedTriggers());

        Assert.True(machine.TryFire(UnreachableTrigger.Connect));
        Assert.Equal(UnreachableState.Connected, machine.CurrentState);

        Assert.False(machine.CanFire(UnreachableTrigger.Isolate));
        var permittedFromConnected = machine.GetPermittedTriggers();
        Assert.DoesNotContain(UnreachableTrigger.Isolate, permittedFromConnected);
    }
}
