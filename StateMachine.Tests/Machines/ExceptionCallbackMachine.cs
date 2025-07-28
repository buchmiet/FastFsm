using System;
using Abstractions.Attributes;
using static StateMachine.Tests.BasicVariant.StateCallbackTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(ExceptionState), typeof(ExceptionTrigger))]
    public partial class ExceptionCallbackMachine
    {
        public bool ThrowInOnExit { get; set; }
        public bool ThrowInOnEntry { get; set; }

        [State(ExceptionState.A, OnExit = nameof(OnExitA))]
        [State(ExceptionState.B, OnEntry = nameof(OnEntryB))]
        private void ConfigureStates() { }

        [Transition(ExceptionState.A, ExceptionTrigger.Go, ExceptionState.B)]
        private void Configure() { }

        private void OnExitA()
        {
            if (ThrowInOnExit)
                throw new InvalidOperationException("OnExit failed");
        }

        private void OnEntryB()
        {
            if (ThrowInOnEntry)
                throw new InvalidOperationException("OnEntry failed");
        }
    }
}
