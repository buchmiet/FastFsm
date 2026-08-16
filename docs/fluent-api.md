# Fluent API

The Fluent API configures states and transitions inside a `Configure()` (or legacy `SetupStates()`) method on a `partial` class marked with `[StateMachine]`.

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

- `Configure()` must be an **instance** method on the state machine `partial` class (static `Configure()` is a discouraged legacy fallback — see diagnostic FSM3081d).
- Use **method groups** (`nameof(MyGuard)`, `SendConfirmation`) — not lambdas or arbitrary expressions.
- Every `.On(...)` chain must end with `.GoTo(state)` or `.Internal()`.
- Callbacks must be declared on the state machine class (not external helpers referenced directly).

See [diagnostics.md](diagnostics.md) for FSM3000–FSM3083.

## States

```csharp
FSM
    .State(MyState.A)
        .OnEntry(OnEnterA)
        .OnExit(OnLeaveA)
        .On(MyTrigger.Go).Guard(CanGo).Action(DoGo).GoTo(MyState.B)
```

### Chaining multiple states

Chain `.State(...)` calls without a separator method:

```csharp
FSM
    .State(S.One).On(T.Next).GoTo(S.Two)
    .State(S.Two).On(T.Next).GoTo(S.Three);
```

> **Note:** Older drafts used `.And()` between states. The current DSL does not expose `.And()` — chain `.State()` directly.

## Guards and actions

```csharp
.On(T.Start)
    .Guard(nameof(IsReady))      // bool IsReady()
    .Action(nameof(Begin))       // void Begin()
    .GoTo(S.Running)
```

Async callbacks (`ValueTask`, `ValueTask<bool>`) produce async machines — see [async.md](async.md).

## Internal transitions

Stay in the same state (no exit/entry), run an action:

```csharp
.On(T.Refresh)
    .Action(nameof(OnRefresh))
    .Internal();
```

Or use `.OnInternal(T.Refresh).Action(...).Internal()` for ancestor-level internal transitions in HSMs.

## Transition priority

When multiple transitions could match, assign priority with an integer literal:

```csharp
.On(T.Event).Priority(10).GoTo(S.High)
// lower-priority alternative on another path...
```

## Hierarchical states (Fluent)

Use `Parent()`, `IsInitial()`, and `WithHistory()` on composite states:

```csharp
[StateMachine(typeof(HState), typeof(HTrigger), EnableHierarchy = true)]
public partial class HsmExample
{
    private void Configure() => FSM
        .State(HState.Composite)
            .Parent(HState.Root)           // substate relationship
            .IsInitial()                   // default child when entering parent
            .WithHistory(HistoryMode.Shallow)
        .State(HState.Leaf)
            .Parent(HState.Composite)
        .State(HState.Root)
            .On(HTrigger.Next).GoTo(HState.Composite);
}
```

| API | Purpose |
|-----|---------|
| `Parent(parentState)` | Declares a substate under a composite parent |
| `IsInitial()` | Marks the default child when the parent is entered |
| `WithHistory(HistoryMode.Shallow \| Deep)` | Enables history on a composite state |

> **Removed / legacy names:** `ChildOf()`, `Initial()`, `HistoryShallow()`, and `HistoryDeep()` are **not** part of the current public Fluent surface. Use `Parent()`, `IsInitial()`, and `WithHistory(...)` instead. The parser may still recognize `ChildOf` as an alias internally, but new code should use `Parent`.

Attribute equivalents: `[State(..., Parent = ..., IsInitial = true, History = ...)]` — see [hsm.md](hsm.md).

## Global exception handler

```csharp
FSM.OnException(nameof(HandleError));

private ExceptionDirective HandleError(ExceptionContext<MyState, MyTrigger> ctx)
{
    return ExceptionDirective.Suppress;
}
```

Only one global `OnException` handler is allowed per machine.

## Attribute API alternative

The same machine can be expressed with `[Transition]`, `[State]`, and related attributes. Both styles are compiled by the same generator. See [attribute-api.md](attribute-api.md).
