using System.Collections.Generic;
using Abstractions.Attributes;
using static StateMachine.Tests.BasicVariant.StateCallbackTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(SelfState), typeof(SelfTrigger))]
    public partial class SelfTransitionMachine
    {
        public List<string> EventLog { get; } = [];

        [State(SelfState.Active,
            OnEntry = nameof(OnEntryActive),
            OnExit = nameof(OnExitActive))]
        private void ConfigureStates() { }

        [Transition(SelfState.Active, SelfTrigger.Refresh, SelfState.Active,
            Action = nameof(RefreshAction))]
        private void Configure() { }

        private void OnEntryActive() => EventLog.Add("OnEntry-Active");
        private void OnExitActive() => EventLog.Add("OnExit-Active");
        private void RefreshAction() => EventLog.Add("RefreshAction");
    }
}
