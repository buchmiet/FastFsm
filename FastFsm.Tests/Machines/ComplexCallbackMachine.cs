using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using StateMachine.Tests.Features.Core;


namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.ComplexCallbackState), typeof(StateCallbackTests.ComplexCallbackTrigger))]
    public partial class ComplexCallbackMachine
    {
        public List<string> EventSequence { get; } = [];
        public bool ResourcesCleaned { get; private set; }
        public DateTime? CompletionTime { get; private set; }

        [State(StateCallbackTests.ComplexCallbackState.Idle,
            OnEntry = nameof(OnEnterIdle),
            OnExit = nameof(OnExitIdle))]
        [State(StateCallbackTests.ComplexCallbackState.Ready,
            OnEntry = nameof(OnEnterReady),
            OnExit = nameof(OnExitReady))]
        [State(StateCallbackTests.ComplexCallbackState.Processing,
            OnEntry = nameof(OnEnterProcessing),
            OnExit = nameof(OnExitProcessing))]
        [State(StateCallbackTests.ComplexCallbackState.Done,
            OnEntry = nameof(OnEnterDone))]
        private void ConfigureStates() { }

        [Transition(StateCallbackTests.ComplexCallbackState.Idle, StateCallbackTests.ComplexCallbackTrigger.Start,
            StateCallbackTests.ComplexCallbackState.Ready)]
        [Transition(StateCallbackTests.ComplexCallbackState.Ready, StateCallbackTests.ComplexCallbackTrigger.Process,
            StateCallbackTests.ComplexCallbackState.Processing)]
        [Transition(StateCallbackTests.ComplexCallbackState.Processing, StateCallbackTests.ComplexCallbackTrigger.Complete,
            StateCallbackTests.ComplexCallbackState.Done)]
        private void Configure() { }

        private void OnEnterIdle() => EventSequence.Add("Entry-Idle");
        private void OnExitIdle() => EventSequence.Add("Exit-Idle");
        private void OnEnterReady() => EventSequence.Add("Entry-Ready");
        private void OnExitReady() => EventSequence.Add("Exit-Ready");
        private void OnEnterProcessing() => EventSequence.Add("Entry-Processing");
        private void OnExitProcessing()
        {
            EventSequence.Add("Exit-Processing");
            ResourcesCleaned = true;
        }
        private void OnEnterDone()
        {
            EventSequence.Add("Entry-Done");
            CompletionTime = DateTime.Now;
        }
    }
}
