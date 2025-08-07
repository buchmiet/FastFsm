using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace StateMachine.Async.Tests
{
    public class RcRaceConditionControlledTests
    {
        [Fact]
        public async Task Parallel_fires_are_serialized_only_one_transition_succeeds_and_callbacks_run_once()
        {
            // Arrange
            RcMachine.ResetConcurrencyProbe();
            var m = new RcMachine(RcStates.Initial);
            await m.StartAsync();

            // Dwa równoległe przejścia z tego samego stanu źródłowego
            var t1 = m.TryFireAsync(RcTriggers.ToA);
            var t2 = m.TryFireAsync(RcTriggers.ToB);

            // 1) Czekamy aż PIERWSZE wywołanie wejdzie do SlowActionAsync
            await RcMachine.WaitUntilFirstInsideAsync(TimeSpan.FromSeconds(5));

            // 2) W tym momencie tylko jedno wywołanie jest w środku
            m.SlowActionCalls.ShouldBe(1, "Dzięki serializacji tylko jedna ścieżka powinna być w SlowActionAsync.");

            // 3) Zwolnij barierę, pozwalając pierwszemu dokończyć
            RcMachine.ReleaseFirst();

            // 4) Obie operacje kończą – jedna sukces, druga false
            var results = await Task.WhenAll(t1.AsTask(), t2.AsTask());
            results.Count(x => x).ShouldBe(1, "Tylko jedno z dwóch równoległych przejść powinno się udać.");

            // 5) Callbacks: OnExit raz, OnEntry (A albo B) raz, SlowAction raz
            m.OnExitCalls.ShouldBe(1);
            (m.OnEntryACalls + m.OnEntryBCalls).ShouldBe(1);
            m.SlowActionCalls.ShouldBe(1);

            // 6) Stan końcowy to A lub B (zależnie od tego, kto wygrał)
            m.CurrentState.ShouldBeOneOf(RcStates.A, RcStates.B);
        }
    }
}