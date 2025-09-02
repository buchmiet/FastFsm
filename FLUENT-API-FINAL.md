# FastFSM Fluent API - Final Specification v1.0

## ✅ API Status: FINALIZED

This document represents the **final, frozen API** for FastFSM Fluent configuration.

## Core Principles

1. **Stateless-style async**: Explicit `*Async()` methods + `FireAsync()` requirement ([inspired by Stateless](https://github.com/dotnet-state-machine/stateless))
2. **No lambdas**: Only `nameof()` and `typeof()` for source generator compatibility
3. **Typed builders**: Compile-time safety with generic constraints
4. **Payload per trigger**: Similar to Stateless' parameterized triggers

## API Reference

### Entry Points

```csharp
public static class FSM
{
    public static StateBuilder<TState> State<TState>(TState state) where TState : Enum;
    public static StateBuilder<TState> At<TState>(TState state) where TState : Enum; // alias
}
```

### StateBuilder

Configures state-level behavior:

```csharp
public sealed class StateBuilder<TState> where TState : Enum
{
    // Entry/Exit actions
    public StateBuilder<TState> OnEntry(string methodName);       // sync
    public StateBuilder<TState> OnEntryAsync(string methodName);  // async
    public StateBuilder<TState> OnExit(string methodName);        // sync
    public StateBuilder<TState> OnExitAsync(string methodName);   // async

    // Transitions
    public TransitionBuilder<TState, TTrigger> On<TTrigger>(TTrigger trigger);
    public TransitionBuilder<TState, TTrigger> OnInternal<TTrigger>(TTrigger trigger);
    
    // Navigation
    public StateBuilder<TState> State(TState state); // define another state
}
```

### TransitionBuilder

Configures transition-level behavior:

```csharp
public sealed class TransitionBuilder<TState, TTrigger> where TState : Enum where TTrigger : Enum
{
    // Payload
    public TransitionBuilder<TState, TTrigger> Payload(Type type);
    public TransitionBuilder<TState, TTrigger> Payload<TPayload>();
    
    // Guards
    public TransitionBuilder<TState, TTrigger> Guard(string methodName);
    public TransitionBuilder<TState, TTrigger> GuardAsync(string methodName);
    
    // Actions
    public TransitionBuilder<TState, TTrigger> Action(string methodName);
    public TransitionBuilder<TState, TTrigger> ActionAsync(string methodName);
    
    // Finalization
    public StateBuilder<TState> GoTo(TState targetState);  // external transition
    public StateBuilder<TState> Internal();                // internal transition
    
    // Continue (auto-finalizes previous as internal)
    public TransitionBuilder<TState, TTrigger> On(TTrigger trigger);
    public TransitionBuilder<TState, TTrigger> OnInternal(TTrigger trigger);
    public StateBuilder<TState> State(TState state);
}
```

## Triggering Transitions

### Generated API

```csharp
// Sync - only for fully synchronous paths
public bool Fire(WorkflowTrigger trigger);
public bool Fire<TPayload>(WorkflowTrigger trigger, TPayload payload);

// Async - required when any async handler exists
public Task<bool> FireAsync(WorkflowTrigger trigger, CancellationToken ct = default);
public Task<bool> FireAsync<TPayload>(WorkflowTrigger trigger, TPayload payload, CancellationToken ct = default);
```

### Rules

- If **any** handler in the path is async → must use `FireAsync()`
- Using `Fire()` on async path → **Warning** (Implicit) or **Error** (Explicit/Required)
- This matches Stateless behavior exactly

## Async Policy

Configured via `[StateMachine(..., AsyncPolicy = AsyncPolicy.Implicit)]`:

| Policy | Description | Fire() on async path | Async without *Async() |
|--------|-------------|---------------------|------------------------|
| **Implicit** (default) | Infers from signatures | Warning | Allowed |
| **Explicit** | Requires *Async() suffix | Error | Error |
| **Required** | All handlers must be async | Not available | Error |

## CancellationToken Propagation

Priority order:
1. `FireAsync(..., ct)` parameter
2. `machine.CancellationToken` property
3. No token (methods called without CT)

Injected only where handler signature accepts it:
- `Task ActionAsync(CancellationToken ct)`
- `Task<bool> GuardAsync(TPayload payload, CancellationToken ct)`

## Allowed Handler Signatures

### Without Payload

```csharp
// Actions
void Action()
Task ActionAsync()
Task ActionAsync(CancellationToken ct)

// Guards
bool Guard()
Task<bool> GuardAsync()
Task<bool> GuardAsync(CancellationToken ct)

// Entry/Exit
void OnEntry()
Task OnEntryAsync()
Task OnEntryAsync(CancellationToken ct)
```

### With Payload

```csharp
// Actions
void Action(TPayload payload)
Task ActionAsync(TPayload payload)
Task ActionAsync(TPayload payload, CancellationToken ct)

// Guards
bool Guard(TPayload payload)
Task<bool> GuardAsync(TPayload payload)
Task<bool> GuardAsync(TPayload payload, CancellationToken ct)
```

## Validation Rules

| Scenario | Diagnostic |
|----------|------------|
| Open transition (no GoTo/Internal) | Error |
| Multiple `On()` with open transition | Auto-finalize as Internal + Warning |
| Multiple `.Payload()` on same transition | Warning (last wins) |
| Incompatible handler signature | Error (no overload) / Warning (has alternative) |
| Async path + `Fire()` (Implicit) | Warning "Use FireAsync" |
| Async path + `Fire()` (Explicit/Required) | Error |

## Complete Example

```csharp
[StateMachine(typeof(WorkflowState), typeof(WorkflowTrigger))]
public partial class WorkflowMachine
{
    private static void Configure() => FSM
        .State(WorkflowState.Idle)
            .OnEntryAsync(nameof(OnIdleEntryAsync))
            .On(WorkflowTrigger.Start)
                .Payload<JobData>()
                .GuardAsync(nameof(CanStartAsync))
                .ActionAsync(nameof(StartJobAsync))
                .GoTo(WorkflowState.Processing)
                
        .State(WorkflowState.Processing)
            .OnEntryAsync(nameof(OnProcessingEnterAsync))
            .OnExitAsync(nameof(OnProcessingExitAsync))
            .OnInternal(WorkflowTrigger.UpdateProgress)
                .Payload<JobData>()
                .Action(nameof(LogProgress))
                .Internal()
            .On(WorkflowTrigger.Finish)
                .Action(nameof(Finalize))
                .GoTo(WorkflowState.Complete)
            .On(WorkflowTrigger.Error)
                .GoTo(WorkflowState.Failed)
                
        .At(WorkflowState.Failed)  // Using At() alias
            .OnEntry(nameof(OnFailedEntry))
            
        .State(WorkflowState.Complete)
            .OnExit(nameof(OnCompleteExit));
    
    // Async guard with payload and CT
    private async Task<bool> CanStartAsync(JobData data, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        return !string.IsNullOrEmpty(data.JobId);
    }
    
    // Async action with payload and CT
    private async Task StartJobAsync(JobData data, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        Console.WriteLine($"Starting job {data.JobId}");
    }
}

// Usage:
var machine = new WorkflowMachine(WorkflowState.Idle);
var result = await machine.FireAsync(WorkflowTrigger.Start, new JobData { JobId = "123" }, ct);
```

## Migration from Old Syntax

### ❌ Old (incorrect)
```csharp
.On(Trigger).GoTo(State).Guard(nameof(Method))  // Guard after GoTo - won't work!
```

### ✅ New (correct)
```csharp
.On(Trigger).Guard(nameof(Method)).GoTo(State)  // Guard before GoTo
```

## Parser Implementation Notes

1. **State context**: Track current state from last `State()` or `At()` call
2. **Open transitions**: Track unfinalized transitions (no `GoTo()`/`Internal()`)
3. **Auto-finalization**: When seeing new `On()` or `State()`, finalize previous as internal + warning
4. **Payload mapping**: `.Payload()` sets both `transition.ExpectedPayloadType` and `model.TriggerPayloadTypes[trigger]`
5. **Async detection**: `*Async()` methods set explicit flag; also infer from method signatures
6. **CT analysis**: Check handler signatures for `CancellationToken` parameter

## Generator Requirements

1. **Fire/FireAsync**: Generate both, but `Fire()` only for fully sync paths
2. **Await handling**: Proper `await` for all async handlers
3. **CT propagation**: Pass token based on priority rules
4. **ConfigureAwait**: Use `ConfigureAwait(!ContinueOnCapturedContext)`
5. **Diagnostics**: Emit warnings/errors based on AsyncPolicy

## Definition of Done

- [x] API matches this specification exactly
- [x] StateBuilder has no Guard/Action methods
- [x] TransitionBuilder has all required methods
- [x] Parser handles all new methods
- [ ] Validation rules implemented
- [ ] Fire/FireAsync generation works
- [ ] CT propagation implemented
- [ ] Tests cover all scenarios
- [ ] README references Stateless patterns

## References

- [Stateless on GitHub](https://github.com/dotnet-state-machine/stateless) - OnEntryAsync/FireAsync pattern
- [Stateless 3.0 Release](https://nblumhardt.com/2016/11/stateless-30/) - Async handlers design
- [Stack Overflow](https://stackoverflow.com/questions/tagged/stateless-state-machine) - Parameterized triggers patterns