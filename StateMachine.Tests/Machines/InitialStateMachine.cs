using System.Collections.Generic;
using Abstractions.Attributes;
using static StateMachine.Tests.BasicVariant.StateCallbackTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(InitialState), typeof(InitialTrigger))]
    public partial class InitialStateMachine
    {
        public List<string> EventLog { get; } = [];

        [State(InitialState.Start, OnEntry = nameof(OnEntryStart), OnExit = nameof(OnExitStart))]
        [State(InitialState.Next, OnEntry = nameof(OnEntryNext))]
        private void ConfigureStates() { }

        [Transition(InitialState.Start, InitialTrigger.Go, InitialState.Next)]
        private void Configure() { }

        private void OnEntryStart() => EventLog.Add("OnEntry-Start");
        private void OnExitStart() => EventLog.Add("OnExit-Start");
        private void OnEntryNext() => EventLog.Add("OnEntry-Next");
    }
}
