using System.Collections.Generic;
using Abstractions.Attributes;
using StateMachine.Contracts;
using Xunit;

namespace StateMachine.Tests.Features.Extensions
{

    // ── mini maszyna ───────────────────────────────────────────────────────────
    [StateMachine(typeof(State), typeof(Trigger), GenerateExtensibleVersion = true)]
    public partial class HookOrderMachine
    {
        private bool Guard() => true;

        [Transition(State.A, Trigger.Next, State.B,
            Guard = nameof(Guard))]
        private void Configure() { }
    }

    public enum State { A, B }
    public enum Trigger { Next }

    // ── extension zbierający zdarzenia ────────────────────────────────────────
    public class RecordingExtension : IStateMachineExtension
    {
        private readonly List<string> _log;
        public RecordingExtension(List<string> log) => _log = log;

        public void OnBeforeTransition<T>(T ctx) where T : IStateMachineContext
            => _log.Add("Before");

        public void OnAfterTransition<T>(T ctx, bool s) where T : IStateMachineContext
            => _log.Add($"After:{(s ? "Success" : "Fail")}");

        public void OnGuardEvaluation<T>(T ctx, string _) where T : IStateMachineContext
            => _log.Add("GuardEval");

        public void OnGuardEvaluated<T>(T ctx, string _, bool res) where T : IStateMachineContext
            => _log.Add("GuardEvaluated");
    }

    public class ExtensionHookOrderTests
    {
        [Fact]
        public void Hooks_AreInvoked_InExpectedOrder()
        {
            // arrange
            var log = new List<string>();
            var ext = new RecordingExtension(log);
            var machine = new HookOrderMachine(State.A, [ext]);
            machine.Start();

            // act
            machine.TryFire(Trigger.Next);

            // assert – pełna sekwencja
            var expected = new[]
            {
                "Before",
                "GuardEval",
                "GuardEvaluated",
                "After:Success"
            };
            Assert.Equal(expected, log);
        }

       
    }
}
