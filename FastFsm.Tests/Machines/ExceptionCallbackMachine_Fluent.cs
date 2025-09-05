using System;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Core;


namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.ExceptionState), typeof(StateCallbackTests.ExceptionTrigger))]
    public partial class ExceptionCallbackMachine_Fluent
    {
        public bool ThrowInOnExit { get; set; }
        public bool ThrowInOnEntry { get; set; }

        private static void Configure() => FSM
            .State<StateCallbackTests.ExceptionState>(StateCallbackTests.ExceptionState.A)
                .OnExit(nameof(OnExitA))
                .On(StateCallbackTests.ExceptionTrigger.Go).GoTo(StateCallbackTests.ExceptionState.B)
            .State(StateCallbackTests.ExceptionState.B)
                .OnEntry(nameof(OnEntryB));

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