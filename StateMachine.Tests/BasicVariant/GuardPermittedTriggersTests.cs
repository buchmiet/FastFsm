using Abstractions.Attributes;
using Xunit;

namespace StateMachine.Tests.BasicVariant
{
    public class GuardPermittedTriggersTests
    {
        [Fact]
        public void PermittedTriggers_ReflectCurrentGuardState()
        {
            var machine = new GuardPermittedMachine(State.Idle)
            {
                // guard początkowo false
                Allow = false
            };
            machine.Start();

            Assert.DoesNotContain(Trigger.Run, machine.GetPermittedTriggers());

            // guard true
            machine.Allow = true;
            Assert.Contains(Trigger.Run, machine.GetPermittedTriggers());
        }

      
    }

    // ── mini-FSM ───────────────────────────────────────────────────────────────
    [StateMachine(typeof(State), typeof(Trigger))]
    public partial class GuardPermittedMachine
    {
        public bool Allow { get; set; }

        private bool CanRun() => Allow;

        [Transition(State.Idle, Trigger.Run, State.Done,
            Guard = nameof(CanRun))]
        private void Configure() { }
    }

    public enum State { Idle, Done }
    public enum Trigger { Run }
}