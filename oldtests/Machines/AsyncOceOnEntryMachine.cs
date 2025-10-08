using System;
using System.Threading;
using System.Threading.Tasks;
using Abstractions.Attributes;
using FastFsm.Exceptions;
using FastFsm.Tests.Features.Exceptions;

namespace FastFsm.Tests.Machines;

[StateMachine(typeof(CSState), typeof(CSTrigger), ContinueOnCapturedContext = false)]
[OnException(nameof(HandleAsync))]
public partial class AsyncOceOnEntryMachine
{
    public bool ThrowOceOnEntryB { get; set; }

    [State(CSState.B, OnEntry = nameof(OnEntryBAsync))]
    [Transition(CSState.A, CSTrigger.Go, CSState.B)]
    private void Configure() { }

    private async ValueTask OnEntryBAsync(CancellationToken ct)
    {
        await Task.Yield();
        if (ThrowOceOnEntryB)
            throw new OperationCanceledException();
    }

    private ValueTask<ExceptionDirective> HandleAsync(ExceptionContext<CSState, CSTrigger> ctx, CancellationToken ct)
        => ValueTask.FromResult(ExceptionDirective.Continue);
}