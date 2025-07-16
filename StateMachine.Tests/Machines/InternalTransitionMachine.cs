using System.Collections.Generic;
using Abstractions.Attributes;
using static StateMachine.Tests.BasicVariant.StateCallbackTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(InternalState), typeof(InternalTrigger))]
    public partial class InternalTransitionMachine
    {
        public List<string> EventLog { get; } = [];

        [State(InternalState.Active,
            OnEntry = nameof(OnEntryActive),
            OnExit = nameof(OnExitActive))]
        [State(InternalState.Inactive,
            OnEntry = nameof(OnEntryInactive))]
        private void ConfigureStates() { }

        [InternalTransition(InternalState.Active, InternalTrigger.Update,
            nameof(HandleUpdate))]
        [Transition(InternalState.Active, InternalTrigger.Deactivate,
            InternalState.Inactive)]
        private void Configure() { }

        private void OnEntryActive() => EventLog.Add("OnEntry-Active");
        private void OnExitActive() => EventLog.Add("OnExit-Active");
        private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
        private void HandleUpdate() => EventLog.Add("InternalAction");
    }
}
