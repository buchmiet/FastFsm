using System.Collections.Generic;
using FastFsm.Contracts;

namespace FastFsm.Tests.Extensions
{
    // ── mini maszyna ───────────────────────────────────────────────────────────
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

        public void OnInternalTransition<TContext>(TContext context) where TContext : IStateMachineContext
            => _log.Add("InternalTransition");

        public void OnUnhandledTrigger<TContext>(TContext context) where TContext : IStateMachineContext
            => _log.Add("UnhandledTrigger");
    }
}
