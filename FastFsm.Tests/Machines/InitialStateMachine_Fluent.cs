using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Core;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.InitialState), typeof(StateCallbackTests.InitialTrigger))]
    public partial class InitialStateMachine_Fluent
    {
        public List<string> EventLog { get; } = [];

        private static void Configure() => FSM
            .State<StateCallbackTests.InitialState>(StateCallbackTests.InitialState.Start)
                .OnEntry(nameof(OnEntryStart))
                .OnExit(nameof(OnExitStart))
                .On(StateCallbackTests.InitialTrigger.Go).GoTo(StateCallbackTests.InitialState.Next)
            .State(StateCallbackTests.InitialState.Next)
                .OnEntry(nameof(OnEntryNext));

        private void OnEntryStart() => EventLog.Add("OnEntry-Start");
        private void OnExitStart() => EventLog.Add("OnExit-Start");
        private void OnEntryNext() => EventLog.Add("OnEntry-Next");
    }
}