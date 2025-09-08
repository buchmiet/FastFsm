using System.Collections.Generic;
using Abstractions.Attributes;
using FastFsm.Tests.Features.Core;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.InitialState), typeof(StateCallbackTests.InitialTrigger))]
    public partial class InitialStateMachine
    {
        public List<string> EventLog { get; } = [];

        [State(StateCallbackTests.InitialState.Start, OnEntry = nameof(OnEntryStart), OnExit = nameof(OnExitStart))]
        [State(StateCallbackTests.InitialState.Next, OnEntry = nameof(OnEntryNext))]
        private void ConfigureStates() { }

        [Transition(StateCallbackTests.InitialState.Start, StateCallbackTests.InitialTrigger.Go, StateCallbackTests.InitialState.Next)]
        private void Configure() { }

        private void OnEntryStart() => EventLog.Add("OnEntry-Start");
        private void OnExitStart() => EventLog.Add("OnExit-Start");
        private void OnEntryNext() => EventLog.Add("OnEntry-Next");
    }
}
