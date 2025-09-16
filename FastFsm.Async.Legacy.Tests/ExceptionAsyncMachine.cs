// ExceptionAsyncMachine.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abstractions.Attributes;
using Abstractions.Fluent;

namespace FastFsm.Async.Tests.Features.Exceptions;
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

    // Fluent API equivalent
    [StateMachine(typeof(ExStates), typeof(ExTriggers))]
    public partial class ExceptionAsyncMachineFluentFsm
    {
        private readonly List<string> _log = new();
        public IReadOnlyList<string> Log => _log;

        private static void Configure() => FSM
            .State(ExStates.Init)
                .On(ExTriggers.GuardBoom)
                    .Guard(nameof(ThrowingGuardAsync))
                    .GoTo(ExStates.Next)
                .On(ExTriggers.ActionBoom)
                    .Guard(nameof(GuardOkAsync))
                    .Action(nameof(ThrowingActionAsync))
                    .GoTo(ExStates.Middle)
                .On(ExTriggers.EntryBoom)
                    .Guard(nameof(GuardOkAsync))
                    .GoTo(ExStates.Next)
            .State(ExStates.Middle)
                .On(ExTriggers.ExitBoom)
                    .Guard(nameof(GuardOkAsync))
                    .GoTo(ExStates.Next)
            .State(ExStates.Next)
                .OnEntryAsync(nameof(ThrowingOnEntryAsync))
            .State(ExStates.Middle)
                .OnExitAsync(nameof(ThrowingOnExitAsync));

        private async ValueTask<bool> ThrowingGuardAsync()
        {
            _log.Add("Guard:Begin");
            await Task.Yield();
            throw new InvalidOperationException("guard failed");
        }

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

        private void NoAction() { }
        private void NoAction2() { }

        private async Task ThrowingOnEntryAsync()
        {
            _log.Add("OnEntry:Begin");
            await Task.Yield();
            throw new InvalidOperationException("on entry failed");
        }

        private async ValueTask ThrowingOnExitAsync()
        {
            _log.Add("OnExit:Begin");
            await Task.Yield();
            throw new InvalidOperationException("on exit failed");
        }
    }
