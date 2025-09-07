using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Contracts;
using Xunit;

namespace FastFsm.Tests.Features.Exceptions;

/// <summary>
/// Sprawdza, że wyjątek w akcji nie zmienia stanu i prawidłowo ustawia wynik - wersja Fluent API.
/// </summary>
public class ActionExceptionTests_Fluent
{
    [Fact]
    public void ActionThrow_DoesNotChangeState_TryFireFalse_FireThrows_ExtensionsNotified()
    {
        // ── arrange ───────────────────────────────────────────────────────────
        var ext = new ResultCapturingExtension_Fluent();
        var machine = new ThrowingActionMachine_Fluent(TestState_Fluent.A, [ext]);
        machine.Start();

        // sanity – przed przejściem
        Assert.Equal(TestState_Fluent.A, machine.CurrentState);

        // ── act + assert 1 – TryFire() zwraca false i stan nie zmieniony ─────
        var ok = machine.TryFire(TestTrigger_Fluent.Go);
        Assert.False(ok);
        Assert.Equal(TestState_Fluent.A, machine.CurrentState);

        // OnAfterTransition powinno być wywołane z success == false
        Assert.Single(ext.Results);
        Assert.False(ext.Results[0]);

        // ── act + assert 2 – Fire() rzuca wyjątek ─────────────────────────────
        Assert.Throws<InvalidOperationException>(() => machine.Fire(TestTrigger_Fluent.Go));
    }

    // ───────────────────────── helpers ──────────────────────────────────────

    private class ResultCapturingExtension_Fluent : IStateMachineExtension
    {
        public List<bool> Results { get; } = [];

        public void OnAfterTransition<T>(T ctx, bool success) where T : IStateMachineContext
            => Results.Add(success);

        public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext { }
        public void OnGuardEvaluation<T>(T ctx, string g) where T : IStateMachineContext { }
        public void OnGuardEvaluated<T>(T ctx, string g, bool r) where T : IStateMachineContext { }
    }
}

/// <summary>
/// Minimalna FSM‑ka – jedyna akcja rzuca wyjątek - wersja Fluent API.
/// </summary>
[StateMachine(typeof(TestState_Fluent), typeof(TestTrigger_Fluent), GenerateExtensibleVersion = true)]
public partial class ThrowingActionMachine_Fluent
{
    private static void Configure() => FSM
        .Extensible<TestState_Fluent>()
        .State(TestState_Fluent.A)
            .On(TestTrigger_Fluent.Go).GoTo(TestState_Fluent.B).Do(nameof(ThrowingAction))
        .State(TestState_Fluent.B);

    public void ThrowingAction() => throw new InvalidOperationException("boom");
}

// enumy muszą być w namespace, żeby atrybuty widziały ich pełną nazwę
public enum TestState_Fluent { A, B }
public enum TestTrigger_Fluent { Go }