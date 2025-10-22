using FastFsm.Exceptions;
using Abstractions.Attributes;

namespace Machines.Tests.Machines.Legacy;

[StateMachine(typeof(ASState), typeof(ASTrigger))]
[OnException(nameof(Handle))]
public partial class ContinueOnActionMachine
{
    [Transition(ASState.A, ASTrigger.Go, ASState.B, Action = nameof(DoWork))]
    private void Configure() { }

    private void DoWork() => throw new InvalidOperationException("boom-in-action");

    private ExceptionDirective Handle(ExceptionContext<ASState, ASTrigger> ctx)
        => ExceptionDirective.Continue;
}
