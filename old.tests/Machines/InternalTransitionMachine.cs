using System.Collections.Generic;
using Abstractions.Attributes;
using FastFsm.Tests.Features.Core;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.InternalState), typeof(StateCallbackTests.InternalTrigger))]
    public partial class InternalTransitionMachine
    {
        public List<string> EventLog { get; } = [];

        [State(StateCallbackTests.InternalState.Active,
            OnEntry = nameof(OnEntryActive),
            OnExit = nameof(OnExitActive))]
        [State(StateCallbackTests.InternalState.Inactive,
            OnEntry = nameof(OnEntryInactive))]
        private void ConfigureStates() { }

        [InternalTransition(StateCallbackTests.InternalState.Active, StateCallbackTests.InternalTrigger.Update,
            Action = nameof(HandleUpdate))]
        [Transition(StateCallbackTests.InternalState.Active, StateCallbackTests.InternalTrigger.Deactivate,
            StateCallbackTests.InternalState.Inactive)]
        private void Configure() { }

        private void OnEntryActive() => EventLog.Add("OnEntry-Active");
        private void OnExitActive() => EventLog.Add("OnExit-Active");
        private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
        private void HandleUpdate() => EventLog.Add("InternalAction");
    }
}
