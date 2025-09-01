using System;
using System.Collections.Generic;
using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines
{
    [StateMachine(typeof(StateCallbackTests.ComplexCallbackState), typeof(StateCallbackTests.ComplexCallbackTrigger))]
    public partial class ComplexCallbackMachineFluentAPI
    {
        public List<string> EventSequence { get; } = [];
        public bool ResourcesCleaned { get; private set; }
        public DateTime? CompletionTime { get; private set; }

        private static void Configure() => FSM
            .State(StateCallbackTests.ComplexCallbackState.Idle)
                .OnEntry(nameof(OnEnterIdle))
                .OnExit(nameof(OnExitIdle))
                .On(StateCallbackTests.ComplexCallbackTrigger.Start)
                    .GoTo(StateCallbackTests.ComplexCallbackState.Ready)
            .State(StateCallbackTests.ComplexCallbackState.Ready)
                .OnEntry(nameof(OnEnterReady))
                .OnExit(nameof(OnExitReady))
                .On(StateCallbackTests.ComplexCallbackTrigger.Process)
                    .GoTo(StateCallbackTests.ComplexCallbackState.Processing)
            .State(StateCallbackTests.ComplexCallbackState.Processing)
                .OnEntry(nameof(OnEnterProcessing))
                .OnExit(nameof(OnExitProcessing))
                .On(StateCallbackTests.ComplexCallbackTrigger.Complete)
                    .GoTo(StateCallbackTests.ComplexCallbackState.Done)
            .State(StateCallbackTests.ComplexCallbackState.Done)
                .OnEntry(nameof(OnEnterDone));

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