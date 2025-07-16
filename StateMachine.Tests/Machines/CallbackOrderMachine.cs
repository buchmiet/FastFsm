using System.Collections.Generic;
using Abstractions.Attributes;
using static StateMachine.Tests.BasicVariant.StateCallbackTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(CallbackState), typeof(CallbackTrigger))]
    public partial class CallbackOrderMachine
    {
        public List<string> ExecutionLog { get; } = [];

        [State(CallbackState.A, OnExit = nameof(OnExitA))]
        [State(CallbackState.B, OnEntry = nameof(OnEntryB), OnExit = nameof(OnExitB))]
        [State(CallbackState.C, OnEntry = nameof(OnEntryC))]
        private void ConfigureStates() { }

        [Transition(CallbackState.A, CallbackTrigger.Next, CallbackState.B,
            Action = nameof(ActionAtoB))]
        [Transition(CallbackState.B, CallbackTrigger.Next, CallbackState.C,
            Action = nameof(ActionBtoC))]
        private void Configure() { }

        private void OnExitA() => ExecutionLog.Add("Exit-A");
        private void OnEntryB() => ExecutionLog.Add("Entry-B");
        private void OnExitB() => ExecutionLog.Add("Exit-B");
        private void OnEntryC() => ExecutionLog.Add("Entry-C");
        private void ActionAtoB() => ExecutionLog.Add("Action-A-to-B");
        private void ActionBtoC() => ExecutionLog.Add("Action-B-to-C");
    }
}
