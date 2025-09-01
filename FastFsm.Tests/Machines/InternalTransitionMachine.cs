using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Core;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.InternalState), typeof(StateCallbackTests.InternalTrigger))]
    public partial class InternalTransitionMachine
    {
        public List<string> EventLog { get; } = [];

        private static void Configure() => FSM
            .State(StateCallbackTests.InternalState.Active)
                .OnEntry(nameof(OnEntryActive)).OnExit(nameof(OnExitActive))
                .OnInternal(StateCallbackTests.InternalTrigger.Update).Action(nameof(HandleUpdate))
                .On(StateCallbackTests.InternalTrigger.Deactivate).GoTo(StateCallbackTests.InternalState.Inactive)
            .State(StateCallbackTests.InternalState.Inactive)
                .OnEntry(nameof(OnEntryInactive));

        private void OnEntryActive() => EventLog.Add("OnEntry-Active");
        private void OnExitActive() => EventLog.Add("OnExit-Active");
        private void OnEntryInactive() => EventLog.Add("OnEntry-Inactive");
        private void HandleUpdate() => EventLog.Add("InternalAction");
    }
}
