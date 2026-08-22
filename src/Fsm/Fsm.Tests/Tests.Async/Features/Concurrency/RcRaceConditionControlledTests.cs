using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Tests.Async.Features.Concurrency;
    public class RcRaceConditionControlledTests
    {
        [Fact]
        public async Task Parallel_fires_are_serialized_only_one_transition_succeeds_and_callbacks_run_once()
        {
            // Arrange
            RcMachineFluentFsm.ResetConcurrencyProbe();
            var m = new RcMachineFluentFsm(RcStates.Initial);
            await m.StartAsync();

            // Two parallel transitions from the same source state
            var t1 = m.TryFireAsync(RcTriggers.ToA);
            var t2 = m.TryFireAsync(RcTriggers.ToB);

            // 1) Wait until the FIRST call enters SlowActionAsync
            await RcMachineFluentFsm.WaitUntilFirstInsideAsync(TimeSpan.FromSeconds(5));

            // 2) At this point only one call is inside
            m.SlowActionCalls.ShouldBe(1, "Thanks to serialization only one path should be inside SlowActionAsync.");

            // 3) Release the barrier, allowing the first call to finish
            RcMachineFluentFsm.ReleaseFirst();

            // 4) Both operations finish – one success, the other false
            var results = await Task.WhenAll(t1.AsTask(), t2.AsTask());
            results.Count(x => x).ShouldBe(1, "Only one of the two parallel transitions should succeed.");

            // 5) Callbacks: OnExit raz, OnEntry (A albo B) raz, SlowAction raz
            m.OnExitCalls.ShouldBe(1);
            (m.OnEntryACalls + m.OnEntryBCalls).ShouldBe(1);
            m.SlowActionCalls.ShouldBe(1);

            // 6) Final state is A or B (depending on who won)
            m.CurrentState.ShouldBeOneOf(RcStates.A, RcStates.B);
        }
    }
