
using Abstractions.Attributes;
using Abstractions.Fluent;
using Xunit;

namespace FastFsm.Tests.Features.Core
{
    public class GuardPermittedTriggersTests
    {
        public enum ApiType { Fluent, Legacy }

        [Theory]
        [InlineData(ApiType.Fluent)]
        [InlineData(ApiType.Legacy)]
        public void PermittedTriggers_ReflectCurrentGuardState(ApiType apiType)
        {
            dynamic machine = apiType == ApiType.Fluent
                ? new GuardPermittedMachineFluent(State.Idle) { Allow = false }
                : new GuardPermittedMachineLegacy(State.Idle) { Allow = false };
            
            machine.Start();

            Assert.DoesNotContain(Trigger.Run, machine.GetPermittedTriggers());

            // guard true
            machine.Allow = true;
            Assert.Contains(Trigger.Run, machine.GetPermittedTriggers());
        }
    }

    // ── Legacy API mini-FSM ───────────────────────────────────────────────────────────────
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class GuardPermittedMachineLegacy
    {
        public bool Allow { get; set; }

        private bool CanRun() => Allow;

        [Transition(State.Idle, Trigger.Run, State.Done,
            Guard = nameof(CanRun))]
        private void Configure() { }
    }

    // ── Fluent API mini-FSM ───────────────────────────────────────────────────────────────
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class GuardPermittedMachineFluent
    {
        public bool Allow { get; set; }

        private bool CanRun() => Allow;

        private static void Configure() => FSM
            .State(State.Idle)
                .On(Trigger.Run)
                    .Guard(nameof(CanRun))
                    .GoTo(State.Done);
    }

    public enum State { Idle, Done }
    public enum Trigger { Run }
}
