using Machines.Tests.Payloads;

namespace Machines.Tests.Machines.Legacy;


[StateMachine(typeof(ExitState), typeof(ExitTrigger))]
[PayloadType(typeof(ExitPayload))]
public partial class ExitCallbackMachine
{
    public bool OnExitCalled { get; private set; }
    public string OnExitPayloadData { get; private set; }

    [State(ExitState.A, OnExit = (OnExitA))]
    private void ConfigureStates() { }

    [Transition(ExitState.A, ExitTrigger.Go, ExitState.B)]
    private void Configure() { }

    private void OnExitA()
    {
        OnExitCalled = true;
        // OnExit cannot receive payload, so OnExitPayloadData remains null
    }
}