using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using static StateMachine.Tests.BasicVariant.StateCallbackTests;

namespace StateMachine.Tests.Machines
{
    [StateMachine(typeof(ComplexCallbackState), typeof(ComplexCallbackTrigger))]
    public partial class ComplexCallbackMachine
    {
        public List<string> EventSequence { get; } = [];
        public bool ResourcesCleaned { get; private set; }
        public DateTime? CompletionTime { get; private set; }

        [State(ComplexCallbackState.Idle,
            OnEntry = nameof(OnEnterIdle),
            OnExit = nameof(OnExitIdle))]
        [State(ComplexCallbackState.Ready,
            OnEntry = nameof(OnEnterReady),
            OnExit = nameof(OnExitReady))]
        [State(ComplexCallbackState.Processing,
            OnEntry = nameof(OnEnterProcessing),
            OnExit = nameof(OnExitProcessing))]
        [State(ComplexCallbackState.Done,
            OnEntry = nameof(OnEnterDone))]
        private void ConfigureStates() { }

        [Transition(ComplexCallbackState.Idle, ComplexCallbackTrigger.Start,
            ComplexCallbackState.Ready)]
        [Transition(ComplexCallbackState.Ready, ComplexCallbackTrigger.Process,
            ComplexCallbackState.Processing)]
        [Transition(ComplexCallbackState.Processing, ComplexCallbackTrigger.Complete,
            ComplexCallbackState.Done)]
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
