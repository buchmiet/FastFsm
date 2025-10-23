using System;
using Machines.Tests.Extensions;
using Machines.Tests.Machines;
using Machines.Tests.Machines.Legacy;
using Xunit;

namespace FastFsm.Tests.Exceptions;

/// <summary>
/// Sprawdza, że wyjątek w akcji nie zmienia stanu i prawidłowo ustawia wynik.
/// </summary>
public class ActionExceptionTests
{
    [Fact]
    public void ActionThrow_DoesNotChangeState_TryFireFalse_FireThrows_ExtensionsNotified()
    {
        // ── arrange ───────────────────────────────────────────────────────────
        var ext = new ResultCapturingExtension();
        var machine = new ThrowingActionMachine(ThrowingActionMachine_TestState.A, [ext]);
        machine.Start();

        // sanity – przed przejściem
        Assert.Equal(ThrowingActionMachine_TestState.A, machine.CurrentState);

        // ── act + assert 1 – TryFire() zwraca false i stan nie zmieniony ─────
        var ok = machine.TryFire(TestTrigger.Go);
        Assert.False(ok);
        Assert.Equal(ThrowingActionMachine_TestState.A, machine.CurrentState);

        // OnAfterTransition powinno być wywołane z success == false
        Assert.Single(ext.Results);
        Assert.False(ext.Results[0]);

        // ── act + assert 2 – Fire() rzuca wyjątek ─────────────────────────────
        Assert.Throws<InvalidOperationException>(() => machine.Fire(TestTrigger.Go));
    }



}

