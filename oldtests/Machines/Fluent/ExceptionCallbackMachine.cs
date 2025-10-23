using System;
using Abstractions.Fluent;
using FastFsm.Tests.Machines.Legacy;

namespace FastFsm.Tests.Machines.Fluent;

[StateMachine(typeof(ExceptionState), typeof(ExceptionTrigger))]
public partial class ExceptionCallbackMachine
{
    public bool ThrowInOnExit { get; set; }
    public bool ThrowInOnEntry { get; set; }

    private void Configure() => FSM
        .State<ExceptionState>(ExceptionState.A)
        .OnExit(nameof(OnExitA))
        .On(ExceptionTrigger.Go).GoTo(ExceptionState.B)
        .State(ExceptionState.B)
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