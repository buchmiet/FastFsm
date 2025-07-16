using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using StateMachine.Contracts;
using Xunit;

namespace StateMachine.Tests.BasicVariant;

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
        var machine = new ThrowingActionMachine(TestState.A, [ext]);

        // sanity – przed przejściem
        Assert.Equal(TestState.A, machine.CurrentState);

        // ── act + assert 1 – TryFire() zwraca false i stan nie zmieniony ─────
        var ok = machine.TryFire(TestTrigger.Go);
        Assert.False(ok);
        Assert.Equal(TestState.A, machine.CurrentState);

        // OnAfterTransition powinno być wywołane z success == false
        Assert.Single(ext.Results);
        Assert.False(ext.Results[0]);

        // ── act + assert 2 – Fire() rzuca wyjątek ─────────────────────────────
        Assert.Throws<InvalidOperationException>(() => machine.Fire(TestTrigger.Go));
    }

    // ───────────────────────── helpers ──────────────────────────────────────

    private class ResultCapturingExtension : IStateMachineExtension
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
/// Minimalna FSM‑ka – jedyna akcja rzuca wyjątek.
/// </summary>
[StateMachine(typeof(TestState), typeof(TestTrigger), GenerateExtensibleVersion = true)]
public partial class ThrowingActionMachine
{
    [Transition(TestState.A, TestTrigger.Go, TestState.B, Action = nameof(ThrowingAction))]
    private void Configure() { }

    public void ThrowingAction() => throw new InvalidOperationException("boom");
}

// enumy muszą być w namespace, żeby atrybuty widziały ich pełną nazwę
public enum TestState { A, B }
public enum TestTrigger { Go }
