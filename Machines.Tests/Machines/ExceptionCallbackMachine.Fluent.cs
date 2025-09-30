using Abstractions.Attributes;
using Abstractions.Fluent;
using Machines.Tests.Features.Core;

namespace Machines.Tests.Machines;

[StateMachine(typeof(ExceptionState), typeof(ExceptionTrigger))]
public partial class ExceptionCallbackMachineFluent
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