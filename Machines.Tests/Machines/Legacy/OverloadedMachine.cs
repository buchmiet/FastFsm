using Machines.Tests.Payloads;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;

public class OverloadedMachinePayload
{
    public string Data { get; set; }
}

[StateMachine(typeof(OverloadState), typeof(OverloadTrigger))]
[PayloadType(typeof(OverloadPayload))]
public partial class OverloadedMachine
{
    public List<string> CallLog { get; } = [];

    [State(OverloadState.B, OnEntry = (OnEntryB))]
    private void ConfigureStates() { }

    [Transition(OverloadState.A, OverloadTrigger.Go, OverloadState.B,
        Guard = (CanGo), Action = (DoTransition))]
    private void Configure() { }

    // Parameterless versions
    private bool CanGo()
    {
        CallLog.Add("Guard()");
        return true;
    }

    private void DoTransition()
    {
        CallLog.Add("Action()");
    }

    private void OnEntryB()
    {
        CallLog.Add("OnEntry()");
    }

    // Payload overload versions
    private bool CanGo(OverloadPayload payload)
    {
        CallLog.Add("Guard(payload)");
        return true;
    }

    private void DoTransition(OverloadPayload payload)
    {
        CallLog.Add("Action(payload)");
    }

    private void OnEntryB(OverloadPayload payload)
    {
        CallLog.Add("OnEntry(payload)");
    }
}
