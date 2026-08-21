using Abstractions.Attributes;
using Abstractions.Fluent;
using FastFsm.Contracts;
using FastFsm.Exceptions;

namespace Tests.Observability;

public enum ObservabilityFlatState { A, B }

public enum ObservabilityFlatTrigger
{
    Go,
    Reject,
    Missing,
    Internal,
    Self,
    Payload,
    AlternatePayload
}

public sealed record ObservabilityPayload(int Value);

public sealed record AlternateObservabilityPayload(string Value);

[StateMachine(typeof(ObservabilityFlatState), typeof(ObservabilityFlatTrigger), GenerateExtensibleVersion = true)]
public partial class ObservabilityFlatMachine
{
    [Transition(ObservabilityFlatState.A, ObservabilityFlatTrigger.Go, ObservabilityFlatState.B, Guard = nameof(CanGo))]
    [Transition(ObservabilityFlatState.A, ObservabilityFlatTrigger.Reject, ObservabilityFlatState.B, Guard = nameof(CannotGo))]
    [Transition(ObservabilityFlatState.A, ObservabilityFlatTrigger.Self, ObservabilityFlatState.A)]
    [InternalTransition(ObservabilityFlatState.A, ObservabilityFlatTrigger.Internal, Action = nameof(NoOp))]
    private void Configure() { }

    private bool CanGo() => true;
    private bool CannotGo() => false;
    private void NoOp() { }
}

[StateMachine(typeof(ObservabilityFlatState), typeof(ObservabilityFlatTrigger), GenerateExtensibleVersion = true)]
[PayloadType(ObservabilityFlatTrigger.Payload, typeof(ObservabilityPayload))]
[PayloadType(ObservabilityFlatTrigger.AlternatePayload, typeof(AlternateObservabilityPayload))]
public partial class ObservabilityPayloadMachine
{
    [Transition(ObservabilityFlatState.A, ObservabilityFlatTrigger.Payload, ObservabilityFlatState.B)]
    [Transition(ObservabilityFlatState.B, ObservabilityFlatTrigger.AlternatePayload, ObservabilityFlatState.A)]
    private void Configure() { }
}

public enum ObservabilityOutcomeState { A, B, Parent, Child }

public enum ObservabilityOutcomeTrigger { Go, Internal, Self }

public sealed class ObservabilityOutcomeException : Exception;

[StateMachine(typeof(ObservabilityOutcomeState), typeof(ObservabilityOutcomeTrigger), GenerateExtensibleVersion = true)]
public partial class ObservabilityOutcomeMachine
{
    public TransitionStage? FailureStage { get; init; }
    public TransitionStage? CancellationStage { get; init; }
    public bool RejectGuard { get; init; }

    private void Configure() => FSM
        .OnException<ObservabilityOutcomeState>(nameof(HandleException))
        .State(ObservabilityOutcomeState.A)
            .OnExit(nameof(ThrowFromOnExit))
            .On(ObservabilityOutcomeTrigger.Go)
                .Guard(nameof(ThrowFromGuard))
                .Action(nameof(ThrowFromAction))
                .GoTo(ObservabilityOutcomeState.B)
            .OnInternal(ObservabilityOutcomeTrigger.Internal)
                .Action(nameof(ThrowFromAction))
                .Internal()
            .On(ObservabilityOutcomeTrigger.Self)
                .Action(nameof(ThrowFromAction))
                .GoTo(ObservabilityOutcomeState.A)
        .State(ObservabilityOutcomeState.B)
            .OnEntry(nameof(ThrowFromOnEntry));

    private bool ThrowFromGuard()
    {
        ThrowIfRequested(TransitionStage.Guard);
        return !RejectGuard;
    }

    private void ThrowFromOnExit() => ThrowIfRequested(TransitionStage.OnExit);
    private void ThrowFromOnEntry() => ThrowIfRequested(TransitionStage.OnEntry);

    public void ThrowFromAction() => ThrowIfRequested(TransitionStage.Action);

    private void ThrowIfRequested(TransitionStage stage)
    {
        if (CancellationStage == stage)
            throw new OperationCanceledException();

        if (FailureStage == stage)
            throw new ObservabilityOutcomeException();
    }

    private ExceptionDirective HandleException(ExceptionContext<ObservabilityOutcomeState, ObservabilityOutcomeTrigger> context)
        => ExceptionDirective.Propagate;
}

public enum ObservabilityHsmState { Outside, Root, Left, LeftLeaf, Right, RightLeaf }

public enum ObservabilityHsmTrigger
{
    EnterComposite,
    SwitchBranch,
    AncestorTransition,
    Refresh,
    Self,
    Missing
}

[StateMachine(
    typeof(ObservabilityHsmState),
    typeof(ObservabilityHsmTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class ObservabilityHsmSemanticsMachine
{
    [State(ObservabilityHsmState.Root)]
    [State(ObservabilityHsmState.Left, Parent = ObservabilityHsmState.Root, IsInitial = true)]
    [State(ObservabilityHsmState.LeftLeaf, Parent = ObservabilityHsmState.Left, IsInitial = true)]
    [State(ObservabilityHsmState.Right, Parent = ObservabilityHsmState.Root)]
    [State(ObservabilityHsmState.RightLeaf, Parent = ObservabilityHsmState.Right, IsInitial = true)]
    private void ConfigureStates() { }

    [Transition(ObservabilityHsmState.Outside, ObservabilityHsmTrigger.EnterComposite, ObservabilityHsmState.Root)]
    [Transition(ObservabilityHsmState.LeftLeaf, ObservabilityHsmTrigger.SwitchBranch, ObservabilityHsmState.RightLeaf)]
    [Transition(ObservabilityHsmState.Root, ObservabilityHsmTrigger.AncestorTransition, ObservabilityHsmState.RightLeaf)]
    [Transition(ObservabilityHsmState.Root, ObservabilityHsmTrigger.Self, ObservabilityHsmState.Root)]
    private void ConfigureTransitions() { }

    private void NoOp() { }
}

public enum ObservabilityHistoryState { Outside, Composite, First, Second }

public enum ObservabilityHistoryTrigger { Enter, Next, Exit }

[StateMachine(
    typeof(ObservabilityHistoryState),
    typeof(ObservabilityHistoryTrigger),
    GenerateExtensibleVersion = true,
    EnableHierarchy = true)]
public partial class ObservabilityShallowHistoryMachine
{
    [State(ObservabilityHistoryState.Composite, History = Abstractions.Attributes.HistoryMode.Shallow)]
    [State(ObservabilityHistoryState.First, Parent = ObservabilityHistoryState.Composite, IsInitial = true)]
    [State(ObservabilityHistoryState.Second, Parent = ObservabilityHistoryState.Composite)]
    private void ConfigureStates() { }

    [Transition(ObservabilityHistoryState.Outside, ObservabilityHistoryTrigger.Enter, ObservabilityHistoryState.Composite)]
    [Transition(ObservabilityHistoryState.First, ObservabilityHistoryTrigger.Next, ObservabilityHistoryState.Second)]
    [Transition(ObservabilityHistoryState.Composite, ObservabilityHistoryTrigger.Exit, ObservabilityHistoryState.Outside)]
    private void ConfigureTransitions() { }
}

public enum ObservabilityAsyncFlatState { A, B }

public enum ObservabilityAsyncFlatTrigger { Go, Reject, Missing, Internal, Self }

[StateMachine(typeof(ObservabilityAsyncFlatState), typeof(ObservabilityAsyncFlatTrigger), GenerateExtensibleVersion = true)]
public partial class ObservabilityAsyncFlatMachine
{
    [Transition(ObservabilityAsyncFlatState.A, ObservabilityAsyncFlatTrigger.Go, ObservabilityAsyncFlatState.B, Guard = nameof(CanGoAsync), Action = nameof(ActionAsync))]
    [Transition(ObservabilityAsyncFlatState.A, ObservabilityAsyncFlatTrigger.Reject, ObservabilityAsyncFlatState.B, Guard = nameof(CannotGoAsync), Action = nameof(ActionAsync))]
    [Transition(ObservabilityAsyncFlatState.A, ObservabilityAsyncFlatTrigger.Self, ObservabilityAsyncFlatState.A, Action = nameof(ActionAsync))]
    [InternalTransition(ObservabilityAsyncFlatState.A, ObservabilityAsyncFlatTrigger.Internal, Action = nameof(ActionAsync))]
    private void Configure() { }

    private async ValueTask<bool> CanGoAsync()
    {
        await Task.Yield();
        return true;
    }

    private async ValueTask<bool> CannotGoAsync()
    {
        await Task.Yield();
        return false;
    }

    private async ValueTask ActionAsync() => await Task.Yield();
}

public enum CoexistenceState { A, B }

public enum CoexistenceTrigger { Go }

[StateMachine(typeof(CoexistenceState), typeof(CoexistenceTrigger), GenerateExtensibleVersion = true)]
public partial class CoexistenceMachine
{
    [Transition(CoexistenceState.A, CoexistenceTrigger.Go, CoexistenceState.B)]
    private void Configure() { }
}

public enum DiObservabilityState { A, B }

public enum DiObservabilityTrigger { Go }

[StateMachine(typeof(DiObservabilityState), typeof(DiObservabilityTrigger), GenerateExtensibleVersion = true)]
public partial class DiObservabilityMachine
{
    [Transition(DiObservabilityState.A, DiObservabilityTrigger.Go, DiObservabilityState.B)]
    private void Configure() { }
}
