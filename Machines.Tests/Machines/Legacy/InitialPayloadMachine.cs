using Machines.Tests.Payloads;

namespace Machines.Tests.Machines.Legacy;

public class InitialMachinePayload
{
    public string Data { get; set; }
}

[StateMachine(typeof(InitialPayloadState), typeof(InitialPayloadTrigger))]
[PayloadType(typeof(OverloadPayload))]
public partial class InitialPayloadMachine
{
    public bool InitialEntryCalledParameterless { get; private set; }
    public bool InitialEntryCalledWithPayload { get; private set; }

    [State(InitialPayloadState.Start, OnEntry = (OnEntryStart))]
    private void ConfigureStates() { }

    [Transition(InitialPayloadState.Start, InitialPayloadTrigger.Go, InitialPayloadState.Next)]
    private void Configure() { }

    private void OnEntryStart()
    {
        InitialEntryCalledParameterless = true;
    }
}