# Fluent API

The Fluent API configures states and transitions inside a `Configure()` method on a `partial` class marked with `[StateMachine]`. `SetupStates()` remains available for compatibility with earlier code.

```csharp
using Abstractions.Fluent;
using Abstractions.Attributes;

[StateMachine(typeof(OrderState), typeof(OrderTrigger))]
public partial class OrderWorkflow
{
    private void Configure() => FSM
        .State(OrderState.New)
            .On(OrderTrigger.Submit).GoTo(OrderState.Submitted)
        .State(OrderState.Submitted)
            .OnEntry(SendConfirmation)
            .On(OrderTrigger.Ship).GoTo(OrderState.Shipped);

    private void SendConfirmation() { }
}
```

## Rules enforced at compile time

- `Configure()` must be an instance method on the state-machine `partial` class. Static `Configure()` is accepted for compatibility and is reported by diagnostic `FSM3081d`.
- Callback references must use forms supported by the DSL, such as a method group (`SendConfirmation`) or `nameof(...)` where a string overload is provided. Lambdas and arbitrary expressions are not accepted as callback definitions.
- Every `.On(...)` chain must end with `.GoTo(state)` or `.Internal()`.
- Callback methods must be declared on the state-machine class.

See [diagnostics.md](diagnostics.md) for FSM3000–FSM3083.

## States

```csharp
FSM
    .State(MyState.A)
        .OnEntry(OnEnterA)
        .OnExit(OnLeaveA)
        .On(MyTrigger.Go).Guard(CanGo).Action(DoGo).GoTo(MyState.B);
```

### Chaining multiple states

Chain `.State(...)` calls directly:

```csharp
FSM
    .State(S.One).On(T.Next).GoTo(S.Two)
    .State(S.Two).On(T.Next).GoTo(S.Three);
```

Earlier drafts used `.And()` between states. `.And()` is not part of the current public DSL.

## Guards and actions

```csharp
.On(T.Start)
    .Guard(nameof(IsReady))
    .Action(nameof(Begin))
    .GoTo(S.Running);
```

Asynchronous callbacks using `ValueTask` or `ValueTask<bool>` cause the generated machine to use asynchronous execution paths. See [async.md](async.md).

## Internal transitions

An internal transition handles the trigger without leaving and re-entering the state:

```csharp
.On(T.Refresh)
    .Action(nameof(OnRefresh))
    .Internal();
```

For HSM ancestor-level internal transitions, use `.OnInternal(T.Refresh).Action(...).Internal()`.

## Transition priority

When multiple transitions can match, assign an integer priority:

```csharp
.On(T.Event).Priority(10).GoTo(S.High);
```

See [hsm.md](hsm.md) for transition-selection semantics in hierarchical machines.

## Hierarchical states

Use `Parent()`, `IsInitial()`, and `WithHistory()` to define hierarchy metadata:

```csharp
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
public partial class HsmExample
{
    private void Configure() => FSM
        .State(HState.Composite)
            .WithHistory(HistoryMode.Shallow)
        .State(HState.ChildA)
            .Parent(HState.Composite)
            .IsInitial()
        .State(HState.ChildB)
            .Parent(HState.Composite)
        .State(HState.Composite)
            .On(HTrigger.Next).GoTo(HState.ChildB);
}
```

| API | Purpose |
|-----|---------|
| `Parent(parentState)` | Declares the parent state |
| `IsInitial()` | Marks the child selected when the parent is entered without applicable history |
| `WithHistory(HistoryMode.Shallow | Deep)` | Enables history for a composite state |

`ChildOf()`, `Initial()`, `HistoryShallow()`, and `HistoryDeep()` are not part of the current public Fluent API. The current equivalents are `Parent()`, `IsInitial()`, and `WithHistory(...)`.

The Attribute API expresses the same metadata with `[State(..., Parent = ..., IsInitial = true, History = ...)]`. See [hsm.md](hsm.md).

## Global exception handler

```csharp
FSM.OnException(nameof(HandleError));

private ExceptionDirective HandleError(ExceptionContext<MyState, MyTrigger> ctx)
{
    return ExceptionDirective.Suppress;
}
```

A machine can define one global `OnException` handler.

## Relation to the Attribute API

The same generator backend processes Fluent and attribute configuration. The Attribute API expresses transitions and state metadata with `[Transition]`, `[State]`, and related attributes. See [attribute-api.md](attribute-api.md).
