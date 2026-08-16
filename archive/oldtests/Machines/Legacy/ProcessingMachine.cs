using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Payloads;

namespace FastFsm.Tests.Machines.Legacy;



[StateMachine(typeof(ProcessingState), typeof(ProcessingTrigger))]
[PayloadType(typeof(ProcessConfig))]
public partial class ProcessingMachine
{
    public int ActiveThreads { get; private set; }
    public int Timeout { get; private set; }
    public bool IsInitialized { get; private set; }

    [State(ProcessingState.Running, OnEntry = nameof(InitializeProcessing))]
    private void ConfigureStates() { }

    [Transition(ProcessingState.Idle, ProcessingTrigger.Start, ProcessingState.Running)]
    private void Configure() { }

    private void InitializeProcessing(ProcessConfig config)
    {
        ActiveThreads = config.ThreadCount;
        Timeout = config.TimeoutSeconds;
        IsInitialized = true;
    }
}