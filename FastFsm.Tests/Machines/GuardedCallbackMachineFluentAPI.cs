using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using static FastFsm.Tests.Features.Core.StateCallbackTests;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(GuardedState), typeof(GuardedTrigger))]
    public partial class GuardedCallbackMachineFluentAPI
    {
        public bool AllowTransition { get; set; }
        public List<string> EventLog { get; } = [];

        private static void Configure() => FSM
            .State(GuardedState.A)
                .OnEntry(nameof(OnEntryA))
                .OnExit(nameof(OnExitA))
                .On(GuardedTrigger.Go)
                    .Guard(nameof(CanTransition))
                    .GoTo(GuardedState.B)
            .State(GuardedState.B)
                .OnEntry(nameof(OnEntryB));

        private bool CanTransition() => AllowTransition;
        private void OnEntryA() => EventLog.Add("OnEntry-A");
        private void OnExitA() => EventLog.Add("OnExit-A");
        private void OnEntryB() => EventLog.Add("OnEntry-B");
    }
}