using FastFsm.Exceptions;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(PSState), typeof(PSTrigger))]
[OnException(nameof(Handle))]
public partial class PropagateOnActionMachine
{
    [Transition(PSState.A, PSTrigger.Go, PSState.B, Action = nameof(DoWork))]
    private void Configure() { }

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<PSState, PSTrigger> ctx)
        => ExceptionDirective.Propagate;
}
