namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(ExceptionState), typeof(ExceptionTrigger))]
public partial class ExceptionCallbackMachine
{
    public bool ThrowInOnExit { get; set; }
    public bool ThrowInOnEntry { get; set; }

    [State(ExceptionState.A, OnExit = (OnExitA))]
    [State(ExceptionState.B, OnEntry = (OnEntryB))]
    private void ConfigureStates() { }

    [Transition(ExceptionState.A, ExceptionTrigger.Go, ExceptionState.B)]
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