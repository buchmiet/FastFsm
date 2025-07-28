using System.Threading;
using System.Threading.Tasks;
using StateMachine.Runtime;

namespace Generator.Async.Tests.Implementations;

// Proste typy enum dla testów
public enum TestState { A, B, C }
public enum TestTrigger { Go, Stop }

/// <summary>
/// Prosta, testowa implementacja AsyncStateMachineBase.
/// Umożliwia kontrolowanie czasu trwania operacji wewnątrz maszyny.
/// </summary>
internal sealed class TestAsyncFsm : AsyncStateMachineBase<TestState, TestTrigger>
{
    // Flaga do śledzenia, czy metoda wewnętrzna została wywołana
    public bool WasInternalLogicCalled { get; private set; }

    // Czas symulowanego opóźnienia wewnątrz sekcji krytycznej
    private readonly int _internalDelayMs;

    public TestAsyncFsm(TestState initialState, int internalDelayMs = 0, bool continueOnCapturedContext = false)
        : base(initialState, continueOnCapturedContext)
    {
        _internalDelayMs = internalDelayMs;
    }

    // Pozostałe abstrakcyjne metody - na razie możemy je zaimplementować minimalnie
    public override ValueTask<bool> CanFireAsync(TestTrigger trigger, CancellationToken cancellationToken = default)
    {
        return new ValueTask<bool>(true);
    }

    public override ValueTask<System.Collections.Generic.IReadOnlyList<TestTrigger>> GetPermittedTriggersAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<System.Collections.Generic.IReadOnlyList<TestTrigger>>(System.Array.Empty<TestTrigger>());
    }

    // Kluczowa metoda do testowania
    protected override async ValueTask<bool> TryFireInternalAsync(
        TestTrigger trigger,
        object? payload,
        CancellationToken cancellationToken)
    {
        WasInternalLogicCalled = true;

        // Symuluj pracę asynchroniczną wewnątrz semafora
        if (_internalDelayMs > 0)
        {
            await Task.Delay(_internalDelayMs, cancellationToken);
        }

        // Zmień stan, aby testy mogły weryfikować zmianę
        _currentState = TestState.B;

        return true;
    }
}