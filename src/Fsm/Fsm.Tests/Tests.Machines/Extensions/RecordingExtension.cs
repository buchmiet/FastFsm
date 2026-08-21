using FastFsm.Contracts;
using Tests.Machines.Machines;

namespace Tests.Machines.Extensions
{
    // ── mini maszyna ───────────────────────────────────────────────────────────
    // ── extension zbierający zdarzenia ────────────────────────────────────────
    public class RecordingExtension : IStateMachineExtension<HookOrderState, HookOrderTrigger>
    {
        private readonly List<string> _log;
        public RecordingExtension(List<string> log) => _log = log;

        public ExtensionHooks Hooks => ExtensionHooks.Transitions | ExtensionHooks.Guards;

        public void OnAttemptStarting(in TransitionAttemptContext<HookOrderState, HookOrderTrigger> attempt)
            => _log.Add("Before");

        public void OnAttemptCompleted(
            in TransitionAttemptContext<HookOrderState, HookOrderTrigger> attempt,
            in TransitionResult<HookOrderState> result)
        {
            if (result.Outcome == TransitionOutcome.Succeeded) _log.Add("Transitioned");
            _log.Add($"After:{(result.Outcome == TransitionOutcome.Succeeded ? "Success" : "Fail")}");
        }

        public void OnGuardEvaluating(
            in TransitionAttemptContext<HookOrderState, HookOrderTrigger> attempt,
            in TransitionInfo<HookOrderState> candidate,
            string _)
            => _log.Add("GuardEval");

        public void OnGuardEvaluated(
            in TransitionAttemptContext<HookOrderState, HookOrderTrigger> attempt,
            in TransitionInfo<HookOrderState> candidate,
            string _,
            bool res)
            => _log.Add("GuardEvaluated");
    }
}
