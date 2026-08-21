using System;
using Tests.Machines.Extensions;
using Tests.Machines.Machines;
using Tests.Machines.Machines.Legacy;
using Xunit;

namespace Tests.Fsm.Exceptions;

/// <summary>
/// Sprawdza, że wyjątek w akcji jest propagowany po zmianie stanu i prawidłowo ustawia wynik.
/// </summary>
public class ActionExceptionTests
{
    [Fact]
    public void ActionThrow_PropagatesFromTryFireAndFire_ExtensionsNotified()
    {
        var tryFireExtension = new ResultCapturingExtension();
        var tryFireMachine = new ThrowingActionMachine(
            ThrowingActionMachine_TestState.A,
            [tryFireExtension]);
        tryFireMachine.Start();

        var tryFireException = Assert.Throws<InvalidOperationException>(
            () => tryFireMachine.TryFire(TestTrigger.Go));

        Assert.Equal("boom", tryFireException.Message);
        Assert.Equal(ThrowingActionMachine_TestState.B, tryFireMachine.CurrentState);
        Assert.Single(tryFireExtension.Results);
        Assert.False(tryFireExtension.Results[0]);

        var fireExtension = new ResultCapturingExtension();
        var fireMachine = new ThrowingActionMachine(
            ThrowingActionMachine_TestState.A,
            [fireExtension]);
        fireMachine.Start();

        var fireException = Assert.Throws<InvalidOperationException>(
            () => fireMachine.Fire(TestTrigger.Go));

        Assert.Equal("boom", fireException.Message);
        Assert.Equal(ThrowingActionMachine_TestState.B, fireMachine.CurrentState);
        Assert.Single(fireExtension.Results);
        Assert.False(fireExtension.Results[0]);
    }
}

