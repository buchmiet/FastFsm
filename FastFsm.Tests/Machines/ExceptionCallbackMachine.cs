using System;
using Abstractions.Attributes;
using FastFsm.Tests.Features.Core;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.ExceptionState), typeof(StateCallbackTests.ExceptionTrigger))]
    public partial class ExceptionCallbackMachine
    {
        public bool ThrowInOnExit { get; set; }
        public bool ThrowInOnEntry { get; set; }

        [State(StateCallbackTests.ExceptionState.A, OnExit = nameof(OnExitA))]
        [State(StateCallbackTests.ExceptionState.B, OnEntry = nameof(OnEntryB))]
        private void ConfigureStates() { }

        [Transition(StateCallbackTests.ExceptionState.A, StateCallbackTests.ExceptionTrigger.Go, StateCallbackTests.ExceptionState.B)]
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
