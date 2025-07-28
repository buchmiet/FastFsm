// ExceptionAsyncMachine.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;

namespace StateMachine.Async.Tests
{
    public enum ExStates { Init, Middle, Next }
    public enum ExTriggers { GuardBoom, ActionBoom, EntryBoom, ExitBoom }

    [StateMachine(typeof(ExStates), typeof(ExTriggers))]
    public partial class ExceptionAsyncMachine
    {
        private readonly List<string> _log = new();
        public IReadOnlyList<string> Log => _log;

        // ---------- GUARD, który rzuca ----------
        [Transition(ExStates.Init, ExTriggers.GuardBoom, ExStates.Next, Guard = nameof(ThrowingGuardAsync))]
        private async ValueTask<bool> ThrowingGuardAsync()
        {
            _log.Add("Guard:Begin");
            await Task.Yield();
            throw new InvalidOperationException("guard failed");
        }

        // ---------- GUARD OK + ACTION rzuca (przechodzimy do Middle – brak OnExit na Init) ----------
        [Transition(ExStates.Init, ExTriggers.ActionBoom, ExStates.Middle,
                    Guard = nameof(GuardOkAsync), Action = nameof(ThrowingActionAsync))]
        private async ValueTask<bool> GuardOkAsync()
        {
            _log.Add("GuardOk");
            await Task.Yield();
            return true;
        }

        private async Task ThrowingActionAsync()
        {
            _log.Add("Action:Begin");
            await Task.Yield();
            throw new InvalidOperationException("action failed");
        }

        // ---------- Przejście, które trafi w OnEntry rzucające ----------
        [Transition(ExStates.Init, ExTriggers.EntryBoom, ExStates.Next, Guard = nameof(GuardOkAsync))]
        private void NoAction() { /* nic */ }

        // ---------- Przejście, które trafi w OnExit rzucające ----------
        [Transition(ExStates.Middle, ExTriggers.ExitBoom, ExStates.Next, Guard = nameof(GuardOkAsync))]
        private void NoAction2() { /* nic */ }

        // ---------- OnEntry rzuca ----------
        [State(ExStates.Next, OnEntry = nameof(ThrowingOnEntryAsync))]
        private async Task ThrowingOnEntryAsync()
        {
            _log.Add("OnEntry:Begin");
            await Task.Yield();
            throw new InvalidOperationException("on entry failed");
        }

        // ---------- OnExit rzuca ----------
        [State(ExStates.Middle, OnExit = nameof(ThrowingOnExitAsync))]
        private async ValueTask ThrowingOnExitAsync()
        {
            _log.Add("OnExit:Begin");
            await Task.Yield();
            throw new InvalidOperationException("on exit failed");
        }
    }
}
