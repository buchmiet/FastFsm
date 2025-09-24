using System;
using System.Collections.Generic;
using Abstractions.Fluent;
using FastFsm.Tests.Features.Core;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(ComplexCallbackState), typeof(ComplexCallbackTrigger))]
public partial class ComplexCallbackMachineFluent
{
    public List<string> EventSequence { get; } = [];
    public bool ResourcesCleaned { get; private set; }
    public DateTime? CompletionTime { get; private set; }

    private void Configure() => FSM
        .State<ComplexCallbackState>(ComplexCallbackState.Idle)
        .OnEntry(nameof(OnEnterIdle))
        .OnExit(nameof(OnExitIdle))
        .On(ComplexCallbackTrigger.Start)
        .GoTo(ComplexCallbackState.Ready)
        .State(ComplexCallbackState.Ready)
        .OnEntry(nameof(OnEnterReady))
        .OnExit(nameof(OnExitReady))
        .On(ComplexCallbackTrigger.Process)
        .GoTo(ComplexCallbackState.Processing)
        .State(ComplexCallbackState.Processing)
        .OnEntry(nameof(OnEnterProcessing))
        .OnExit(nameof(OnExitProcessing))
        .On(ComplexCallbackTrigger.Complete)
        .GoTo(ComplexCallbackState.Done)
        .State(ComplexCallbackState.Done)
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