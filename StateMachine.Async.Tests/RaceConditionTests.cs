//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Shouldly;
//using Xunit;
//using Abstractions.Attributes;

//namespace StateMachine.Async.Tests
//{
//    public enum RcStates { Initial, A, B }
//    public enum RcTriggers { ToA, ToB }

//    // Deklaracja maszyny dla generatora + implementacje hooków i sondy współbieżności
//    [StateMachine(typeof(RcStates), typeof(RcTriggers))]
//    public partial class RcMachine
//    {
//        // Liczniki do asercji (atomowe)
//        private int _onExitCalls;
//        private int _onEntryACalls;
//        private int _onEntryBCalls;

//        public int OnExitCalls => Volatile.Read(ref _onExitCalls);
//        public int OnEntryACalls => Volatile.Read(ref _onEntryACalls);
//        public int OnEntryBCalls => Volatile.Read(ref _onEntryBCalls);

//        // --- Sonda współbieżności (bariera) ---
//        private static int _slowActionEntered;
//        private static TaskCompletionSource<bool> _bothInside =
//            new(TaskCreationOptions.RunContinuationsAsynchronously);
//        private static TaskCompletionSource<bool> _release =
//            new(TaskCreationOptions.RunContinuationsAsynchronously);

//        public static void ResetConcurrencyProbe()
//        {
//            Interlocked.Exchange(ref _slowActionEntered, 0);
//            _bothInside = new(TaskCreationOptions.RunContinuationsAsynchronously);
//            _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
//        }

//        public static async Task WaitUntilBothInsideAsync(TimeSpan timeout)
//        {
//            using var cts = new CancellationTokenSource(timeout);
//            await _bothInside.Task.WaitAsync(cts.Token);
//        }

//        public static void ReleaseParallelSection() => _release.TrySetResult(true);

//        // ----- Hooki stanowe i akcja z atrybutami -----

//        // Wyjście ze stanu Initial
//        [State(RcStates.Initial, OnExit = nameof(OnInitialExitAsync))]
//        private async Task OnInitialExitAsync()
//        {
//            Interlocked.Increment(ref _onExitCalls);
//            await Task.Yield();
//        }

//        // Sztucznie "wolna" akcja – oba przejścia przypięte do TEJ metody
//        [Transition(RcStates.Initial, RcTriggers.ToA, RcStates.A, Action = nameof(SlowActionAsync))]
//        [Transition(RcStates.Initial, RcTriggers.ToB, RcStates.B, Action = nameof(SlowActionAsync))]
//        private async Task SlowActionAsync()
//        {
//            var n = Interlocked.Increment(ref _slowActionEntered);
//            if (n == 2) _bothInside.TrySetResult(true);     // Drugi wjazd: sygnał „obaj w środku”
//            await _bothInside.Task.ConfigureAwait(false);   // Czekamy aż test nakaże ruszyć
//            await _release.Task.ConfigureAwait(false);
//        }

//        [State(RcStates.A, OnEntry = nameof(OnEntryAAsync))]
//        private async Task OnEntryAAsync()
//        {
//            Interlocked.Increment(ref _onEntryACalls);
//            await Task.Yield();
//        }

//        [State(RcStates.B, OnEntry = nameof(OnEntryBAsync))]
//        private async Task OnEntryBAsync()
//        {
//            Interlocked.Increment(ref _onEntryBCalls);
//            await Task.Yield();
//        }
//    }

//    public class RcRaceConditionControlledTests
//    {
//        [Fact]
//        public async Task Two_parallel_transitions_from_same_source_should_not_both_succeed_but_do_due_to_race()
//        {
//            // Arrange
//            RcMachine.ResetConcurrencyProbe();
//            var m = new RcMachine(RcStates.Initial);

//            // Dwa równoległe przejścia z tego samego stanu źródłowego
//            var t1 = m.TryFireAsync(RcTriggers.ToA);
//            var t2 = m.TryFireAsync(RcTriggers.ToB);

//            // Czekamy aż obie ścieżki będą w "SlowActionAsync"
//            await RcMachine.WaitUntilBothInsideAsync(TimeSpan.FromSeconds(5));

//            // Pozwalamy kontynuować (deterministycznie, bez sleepów)
//            RcMachine.ReleaseParallelSection();

//            var results = await Task.WhenAll(t1.AsTask(), t2.AsTask());

//            // --- Oczekiwanie kontraktowe: wygrać powinno tylko jedno przejście ---
//            results.Count(x => x).ShouldBe(1,
//                "Przy poprawnej serializacji tylko jedno z dwóch równoległych przejść ze stanu Initial powinno się udać.");

//            m.OnExitCalls.ShouldBe(1,
//                "OnExit ze stanu Initial powinien zostać wywołany dokładnie raz.");

//            (m.OnEntryACalls + m.OnEntryBCalls).ShouldBe(1,
//                "Powinno nastąpić jedno wejście do stanu docelowego (A albo B), nie oba.");

//            m.CurrentState.ShouldBeOneOf(RcStates.A, RcStates.B);
//        }
//    }
//}
