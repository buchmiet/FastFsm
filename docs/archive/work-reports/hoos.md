# FastFSM Extensions Hooks – Full Infrastructure and Emission Points

This document consolidates the current extensions infrastructure with the exact code paths that emit and run hooks. It is intended as a review artifact to guide future modifications and extensions of the hook system.

Scope:
- Interfaces and runtime executor for hooks (contracts + runner)
- Generator: where and how hook calls are emitted (sync/async, flat/HSM, payload/no-payload, success/failure/no‑transition)
- Extension lifecycle and ordering guarantees
- Extension management in generated machines and DI integration


## Contracts

Source: `FastFsm/Contracts/IStateMachineExtension.cs`

```csharp
namespace FastFsm.Contracts;

/// <summary>
/// Extension interface for adding cross-cutting concerns to state machines
/// </summary>
public interface IStateMachineExtension
{
    /// <summary>
    /// Called before a transition is attempted
    /// </summary>
    void OnBeforeTransition<TContext>(TContext context) 
        where TContext : IStateMachineContext;
    
    /// <summary>
    /// Called after a transition completes
    /// </summary>
    void OnAfterTransition<TContext>(TContext context, bool success) 
        where TContext : IStateMachineContext;
    
    /// <summary>
    /// Called when guard evaluation starts
    /// </summary>
    void OnGuardEvaluation<TContext>(TContext context, string guardName) 
        where TContext : IStateMachineContext;
    
    /// <summary>
    /// Called when guard evaluation completes
    /// </summary>
    void OnGuardEvaluated<TContext>(TContext context, string guardName, bool result) 
        where TContext : IStateMachineContext;

    // IMPLEMENTED (New)
    /// <summary>
    /// Called when a trigger was not handled by any state (after bubbling in HSM).
    /// </summary>
    void OnUnhandledTrigger<TContext>(TContext context)
        where TContext : IStateMachineContext;
}
```

Related context types used to carry state information through hooks:

- `FastFsm/Runtime/StateMachineContext.cs` provides `StateMachineContext<TState,TTrigger>(InstanceId, FromState, Trigger, ToState, Payload)` and implements `IStateMachineContext<TState,TTrigger>` and `IStateSnapshot` for logger-friendly access.


## Runtime Hook Executor

Source: `FastFsm/Runtime/Extensions/ExtensionRunner.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FastFsm.Contracts;

#if FSM_LOGGING_ENABLED
using Microsoft.Extensions.Logging;
#endif

namespace FastFsm.Runtime.Extensions
{
#if FSM_LOGGING_ENABLED
    internal static class ExtensionRunnerLog
{
    /// <summary>
    /// Logs an exception thrown by an extension.
    /// </summary>
    public static void ExtensionError(
        this ILogger logger,
        string extensionType,
        string methodName,
        string instanceId,
        string fromState,
        string trigger,
        string toState,
        Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.Log(
                LogLevel.Error,
                new EventId(1001, nameof(ExtensionError)),
                exception,  // <- param 'exception'
                // ----------- zaktualizowany szablon -------------
                "Extension {ExtensionType} threw exception in {MethodName}. " +
                "ExceptionMessage={ExceptionMessage}. " +
                "InstanceId={InstanceId}, FromState={FromState}, Trigger={Trigger}, ToState={ToState}",
                // ---------------- parametry ---------------------
                extensionType,             // {ExtensionType}
                methodName,                // {MethodName}
                exception.Message,         // {ExceptionMessage}
                instanceId,                // {InstanceId}
                fromState,                 // {FromState}
                trigger,                   // {Trigger}
                toState                    // {ToState}
            );
        }
    }
}
#endif

    /// <summary>
    /// Executes extension hooks and – when <c>FSM_LOGGING_ENABLED</c> is defined –
    /// logs errors to <see cref="ILogger"/>.
    /// </summary>
    public sealed partial class ExtensionRunner
    {
        /// <summary>
        /// Common, logger-less instance for use where
        /// additional objects are not needed.
        /// </summary>
        public static ExtensionRunner Default { get; } = new();

#if FSM_LOGGING_ENABLED
        private readonly ILogger? _logger;

        public ExtensionRunner(ILogger? logger = null)
        {
            _logger = logger;
        }
#else
        public ExtensionRunner() { }
#endif

        private void SafeExecute<TContext>(
            IStateMachineExtension extension,
            TContext context,
            Action<IStateMachineExtension, TContext> action,
            string methodName)
            where TContext : IStateMachineContext
        {
            try
            {
                action(extension, context);
            }
            // Note: compilation-dependent catch header eliminating CS0168 when logging is disabled
#if FSM_LOGGING_ENABLED
            catch (Exception ex)
#else
            catch (Exception)
#endif
            {
#if FSM_LOGGING_ENABLED
                if (_logger?.IsEnabled(LogLevel.Error) == true && context is IStateSnapshot snap)
                {
                    _logger.ExtensionError(
                        extension.GetType().Name,
                        methodName,
                        context.InstanceId,
                        snap.FromState?.ToString() ?? "null",
                        snap.Trigger?.ToString() ?? "null",
                        snap.ToState?.ToString() ?? "null",
                        ex);
                }
#endif
                // Extension error should not interrupt state machine operation.
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnBeforeTransition"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunBeforeTransition<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    static (ext, ctx) => ext.OnBeforeTransition(ctx),
                    nameof(IStateMachineExtension.OnBeforeTransition));
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnAfterTransition"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunAfterTransition<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            bool success)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnAfterTransition(ctx, success),
                    nameof(IStateMachineExtension.OnAfterTransition));
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnGuardEvaluation"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunGuardEvaluation<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            string guardName)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnGuardEvaluation(ctx, guardName),
                    nameof(IStateMachineExtension.OnGuardEvaluation));
            }
        }

        /// <summary>
        /// Calls <see cref="IStateMachineExtension.OnGuardEvaluated"/> for all extensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunGuardEvaluated<TContext>(
            IReadOnlyList<IStateMachineExtension> extensions,
            TContext context,
            string guardName,
            bool result)
            where TContext : IStateMachineContext
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    (ext, ctx) => ext.OnGuardEvaluated(ctx, guardName, result),
                    nameof(IStateMachineExtension.OnGuardEvaluated));
            }
        }

        // IMPLEMENTED (New)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RunUnhandledTrigger(
            IReadOnlyList<IStateMachineExtension> extensions,
            IStateMachineContext context)
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                SafeExecute(
                    extensions[i],
                    context,
                    static (ext, ctx) => ext.OnUnhandledTrigger(ctx),
                    nameof(IStateMachineExtension.OnUnhandledTrigger));
            }
        }
    }
}
```

Notes:
- Hook execution is exception-safe. Exceptions from extensions never break the state machine; they’re swallowed, and optionally logged when `FSM_LOGGING_ENABLED` is defined.
- The runner is instantiated with or without `ILogger`, depending on whether logging was generated.


## Generated Machines: Extension Management

When extensions are enabled (`GenerateExtensibleVersion = true` or Fluent `.Extensible()`), the generator adds fields, a public accessor, and management methods.

Source: `Generator/SourceGenerators/ExtensionsFeatureWriter.cs`

```csharp
namespace Generator.SourceGenerators;

internal sealed class ExtensionsFeatureWriter
{
    public void WriteFields(IndentedStringBuilder.IndentedStringBuilder sb)
    {
        sb.AppendLine("private readonly List<IStateMachineExtension> _extensionsList;");
        sb.AppendLine("private readonly IReadOnlyList<IStateMachineExtension> _extensions;");
        sb.AppendLine("private readonly ExtensionRunner _extensionRunner;");
        sb.AppendLine();
        sb.AppendLine("public IReadOnlyList<IStateMachineExtension> Extensions => _extensions;");
        sb.AppendLine();
    }

    public void WriteConstructorBody(IndentedStringBuilder.IndentedStringBuilder sb, bool generateLogging)
    {
        sb.AppendLine("_extensionsList = extensions?.ToList() ?? new List<IStateMachineExtension>();");
        sb.AppendLine("_extensions = _extensionsList;");
        sb.AppendLine(generateLogging
            ? "_extensionRunner = new ExtensionRunner(_logger);"
            : "_extensionRunner = new ExtensionRunner();");
    }

    public void WriteManagementMethods(IndentedStringBuilder.IndentedStringBuilder sb)
    {
        using (sb.Block("public void AddExtension(IStateMachineExtension extension)"))
        {
            sb.AppendLine("if (extension == null) throw new ArgumentNullException(nameof(extension));");
            sb.AppendLine("_extensionsList.Add(extension);");
        }
        sb.AppendLine();

        using (sb.Block("public bool RemoveExtension(IStateMachineExtension extension)"))
        {
            sb.AppendLine("if (extension == null) return false;");
            sb.AppendLine("return _extensionsList.Remove(extension);");
        }
    }
}
```

Constructor parameter injection and initialization (excerpt):

Source: `Generator/SourceGenerators/UnifiedStateMachineGenerator.cs` (method `WriteConstructor`)

```csharp
// ...
if (ExtensionsOn)
{
    extras.Add("IEnumerable<IStateMachineExtension>? extensions = null");
}
// ...
using (Sb.Block($"public {className}({string.Join(", ", paramList)}) : {baseCall}"))
{
    // ...
    if (ExtensionsOn)
    {
        _ext.WriteConstructorBody(Sb, ShouldGenerateLogging);
    }
}
```


## Generator Emission: Hook Calls (Sync, Flat/HSM, No‑Payload)

When extensions are enabled, the sync path uses a dedicated transition emitter that wraps the whole transition and injects all hooks.

Source: `Generator/SourceGenerators/UnifiedStateMachineGenerator.cs`

Method: `WriteTransitionLogicSyncWithExtensions(...)`

```csharp
// Special sync transition logic for WithExtensions variant
// Wraps entire transition in try-catch to handle exceptions properly
private void WriteTransitionLogicSyncWithExtensions(TransitionModel transition, string stateTypeForUsage, string triggerTypeForUsage)
{
    var hasOnEntryExit = ShouldGenerateOnEntryExit();

    // Create context for hooks
    Sb.AppendLine($"var smCtx = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
    Sb.AppendLine("    Guid.NewGuid().ToString(),");
    Sb.AppendLine($"    {CurrentStateField},");
    Sb.AppendLine("    trigger,");
    Sb.AppendLine($"    {stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
    Sb.AppendLine("    payload);");
    Sb.AppendLine();

    // Log transition started (omitted here)

    // Hook: Before transition
    Sb.AppendLine("_extensionRunner.RunBeforeTransition(_extensions, smCtx);");
    Sb.AppendLine();

    // All transition logic in try-catch
    Sb.AppendLine("try {");

    // Guard check (if present) + guard hooks
    //   _extensionRunner.RunGuardEvaluation(_extensions, smCtx, "GuardName");
    //   var guardResult = GuardMethod();
    //   _extensionRunner.RunGuardEvaluated(_extensions, smCtx, "GuardName", guardResult);
    //   if (!guardResult) { _extensionRunner.RunAfterTransition(_extensions, smCtx, false); return false; }

    // UML-friendly order: OnExit → Action → State change → OnEntry
    // ... (callbacks and state change code)

    Sb.AppendLine("}");
    Sb.AppendLine("catch {");
    Sb.AppendLine("    _extensionRunner.RunAfterTransition(_extensions, smCtx, false);");
    Sb.AppendLine("    return false;");
    Sb.AppendLine("}");

    // Success
    Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, smCtx, true);");
    // Log success (omitted here)
    Sb.AppendLine("return true;");
}
```

No‑transition case is also reported to extensions with `success=false`:

Method: `WriteTryFireStructureWithExtensions(...)` (flat and HSM paths)

```csharp
// Custom implementation that notifies extensions even when no transition is found
if (!Model.Transitions.Any())
{
    // No transitions defined - notify extensions
    Sb.AppendLine("// No transitions defined - notify extensions");
    Sb.AppendLine($"var failCtx = new StateMachineContext<{stateType}, {triggerType}>(");
    Sb.AppendLine("    Guid.NewGuid().ToString(),");
    Sb.AppendLine($"    {CurrentStateField},");
    Sb.AppendLine("    trigger,");
    Sb.AppendLine($"    {CurrentStateField},");
    Sb.AppendLine("    payload);");
    Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, failCtx, false);");
    Sb.AppendLine("return false;");
    return;
}

// ... (HSM or flat switch trees that pick a transition)

// No transition found - notify extensions
Sb.AppendLine("// No matching transition - notify extensions");
Sb.AppendLine($"var noTransitionCtx = new StateMachineContext<{stateType}, {triggerType}>(");
Sb.AppendLine("    Guid.NewGuid().ToString(),");
Sb.AppendLine($"    {CurrentStateField},");
Sb.AppendLine("    trigger,");
Sb.AppendLine($"    {CurrentStateField},");
Sb.AppendLine("    payload);");
Sb.AppendLine("_extensionRunner.RunAfterTransition(_extensions, noTransitionCtx, false);");
Sb.AppendLine("return false;");
```


## Generator Emission: Hook Calls (Async and/or Payload)

The async/payload path uses analogous hook emission via helper methods. Key sites:

Method: `WriteTransitionLogicPayloadAsync(...)`

```csharp
// Payload-aware async transition logic (uses success var + END_TRY_FIRE)
private void WriteTransitionLogicPayloadAsync(
    TransitionModel transition,
    string stateTypeForUsage,
    string triggerTypeForUsage)
{
    var hasOnEntryExit = ShouldGenerateOnEntryExit();

    // Hook: Before transition
    WriteBeforeTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage);

    // Guard check (async-aware, with payload)
    if (!string.IsNullOrEmpty(transition.GuardMethod))
    {
        WriteGuardEvaluationHook(transition, stateTypeForUsage, triggerTypeForUsage);
        GuardGenerationHelper.EmitGuardCheck(...); // async guard invocation
        // Ensure extensions are notified after guard is evaluated (UML-friendly order)
        WriteAfterGuardEvaluatedHook(transition, GuardResultVar, stateTypeForUsage, triggerTypeForUsage);

        Sb.AppendLine($"if (!{GuardResultVar})");
        Sb.AppendLine("{");
        // ... logging
        Sb.AppendLine($"{SuccessVar} = false;");
        WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: false);
        Sb.AppendLine($"goto {EndOfTryFireLabel};");
        Sb.AppendLine("}");
    }

    // OnExit ...
    // State change ...
    // OnEntry ...
    // Action ...

    // Success
    Sb.AppendLine($"{SuccessVar} = true;");

    // Hook: After successful transition
    WriteAfterTransitionHook(transition, stateTypeForUsage, triggerTypeForUsage, success: true);

    Sb.AppendLine($"goto {EndOfTryFireLabel};");
}
```

Hook helper emitters (active only when `ExtensionsOn`):

```csharp
// Extension hooks (emitted only when HasExtensions)
protected override void WriteBeforeTransitionHook(
    TransitionModel transition,
    string stateTypeForUsage,
    string triggerTypeForUsage)
{
    if (!ExtensionsOn) return;
    Sb.AppendLine($"var {HookVarContext} = new StateMachineContext<{stateTypeForUsage}, {triggerTypeForUsage}>(");
    using (Sb.Indent())
    {
        Sb.AppendLine("Guid.NewGuid().ToString(),");
        Sb.AppendLine($"{CurrentStateField},");
        Sb.AppendLine("trigger,");
        Sb.AppendLine($"{stateTypeForUsage}.{TypeHelper.EscapeIdentifier(transition.ToState)},");
        Sb.AppendLine($"{PayloadVar});");
    }
    Sb.AppendLine();
    _smCtxCreated = true;
    Sb.AppendLine($"_extensionRunner.RunBeforeTransition(_extensions, {HookVarContext});");
}

protected override void WriteGuardEvaluationHook(...)
{
    if (!ExtensionsOn) return;
    // ensure smCtx exists ...
    Sb.AppendLine($"_extensionRunner.RunGuardEvaluation(_extensions, {HookVarContext}, \"{transition.GuardMethod}\");");
}

protected override void WriteAfterGuardEvaluatedHook(...)
{
    if (!ExtensionsOn) return;
    Sb.AppendLine($"_extensionRunner.RunGuardEvaluated(_extensions, {HookVarContext}, \"{transition.GuardMethod}\", {guardResultVar});");
}

protected override void WriteAfterTransitionHook(..., bool success)
{
    if (!ExtensionsOn) return;
    Sb.AppendLine($"_extensionRunner.RunAfterTransition(_extensions, {HookVarContext}, {success.ToString().ToLowerInvariant()});");
}
```

Async `TryFireInternalAsync` also includes a failure path that notifies extensions when the attempt overall fails (context built from `OriginalStateVar` and the unchanged `ToState`):

```csharp
// In TryFireInternalAsync(...)
if (!Success)
{
    // ... logging
    var failCtx = new StateMachineContext<TState, TTrigger>(
        Guid.NewGuid().ToString(),
        OriginalState,
        trigger,
        OriginalState,
        payload);
    _extensionRunner.RunAfterTransition(_extensions, failCtx, false);
}
```


## DI Integration and Extension Discovery

The DI package allows registering extensions that are injected into generated machines.

Source: `FastFsm/DependencyInjection/FsmServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddStateMachineExtension<TExtension>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
    where TExtension : class, IStateMachineExtension
{
    services.Add(new ServiceDescriptor(
        typeof(IStateMachineExtension),
        typeof(TExtension),
        lifetime));
    
    return services;
}
```

Source: `Generator.DependencyInjection/FactoryCodeGenerator.cs` (excerpt from Create method)

```csharp
if (_model.HasExtensions)
{
    _sb.AppendLine("// Get all registered extensions from DI");
    _sb.AppendLine($"var extensions = {ServiceProviderField}.GetServices<{StateMachineContractsNamespace}.IStateMachineExtension>();");
    _sb.AppendLine();
    instanceParams.Insert(2, "extensions");
}
```

Generated machine constructors accept `IEnumerable<IStateMachineExtension>? extensions = null`; these are materialized to a local list and exposed via `public IReadOnlyList<IStateMachineExtension> Extensions => _extensions;`.


## Hook Ordering and Semantics

- Ordering per transition attempt (when `TryFire`/`Fire` paths run):
  1) `OnBeforeTransition(context)`
  2) `OnGuardEvaluation(context, guardName)` for each guard evaluated
  3) `OnGuardEvaluated(context, guardName, result)`
  4) `OnAfterTransition(context, success)` with `success=true/false`

- Hook contexts include: machine instance ID, `FromState`, `Trigger`, intended `ToState` (or unchanged `ToState` on failures / no-transition), and optional `Payload`.

- No hook calls are emitted during `CanFire(...)`/`GetPermittedTriggers(...)` (and async variants). This matches the documented behavior (guards may be evaluated there, but hooks are intentionally suppressed for these inspection APIs).

- On any failure path (guard returns false, unhandled trigger, or exception thrown by OnExit/Action/OnEntry when safe-action policy is enabled), `OnAfterTransition(..., success=false)` is emitted. Successful transitions end with `OnAfterTransition(..., success=true)` after all effects.


## Conditional Logging

When logging is enabled for the generated machine, the runtime `ExtensionRunner` is constructed with a logger and logs extension exceptions under `FSM_LOGGING_ENABLED`. Hook emission itself is unaffected by logging flags; only error reporting in the runner differs.


## Summary

The current extensions system consists of a clear contract (`IStateMachineExtension`), an exception-safe runtime dispatcher (`ExtensionRunner`), and generator-emitted hook calls that comprehensively cover all `TryFire`/`Fire` execution paths:
- Sync/Async, Flat/HSM, Payload/No-Payload
- Success, failure, and no‑transition cases
- Ordered and context-rich hook invocations

This file includes verbatim code excerpts from the generator and runtime to aid design review and planned enhancements of the hook system.


## Planned Hooks

The following additions refine observability and HSM introspection. They complement the existing broad `OnAfterTransition(ctx, success)` with more precise, semantically targeted notifications.

Proposed interface additions (signatures as requested; typed to `StateMachineContext<TState,TTrigger>`):

```csharp
// NOWE – obserwacja zmian:
void OnTransitioned<TState, TTrigger>(StateMachineContext<TState,TTrigger> ctx)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;
void OnTransitionCompleted<TState, TTrigger>(StateMachineContext<TState,TTrigger> ctx)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;

// NOWE – sytuacje szczególne:
void OnUnhandledTrigger<TState, TTrigger>(StateMachineContext<TState,TTrigger> ctx)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;
void OnInternalTransition<TState, TTrigger>(StateMachineContext<TState,TTrigger> ctx)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;

// NOWE – HSM:
void OnBubbleToParent<TState, TTrigger>(StateMachineContext<TState,TTrigger> ctx, TState handledAt)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;
void OnInitialSubstateEntered<TState, TTrigger>(StateMachineContext<TState,TTrigger> parentCtx, TState child)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;
void OnHistoryRestore<TState, TTrigger>(StateMachineContext<TState,TTrigger> parentCtx, Abstractions.Attributes.HistoryMode mode, TState restoredState)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;
void OnAncestorPathChanged<TState, TTrigger>(StateMachineContext<TState,TTrigger> ctx, ReadOnlySpan<TState> exitedPath, ReadOnlySpan<TState> enteredPath, TState lca)
    where TState : unmanaged, Enum where TTrigger : unmanaged, Enum;
```

Notes:
- Alternatively, to align with current interface style, these can be generalized to `IStateMachineContext` as parameter type. The above uses the strongly-typed `StateMachineContext<,>` for richer consumers.
- Backward-compatibility: keep `OnAfterTransition(ctx, success)` as the broad, finally-style hook. New hooks provide precise semantics for state change and end-of-cycle.

Planned emission points in the generator (UnifiedStateMachineGenerator):

- OnTransitioned(ctx)
  - When: right after a successful, state-changing transition finishes all effects (OnExit, Action, state assignment, OnEntry), but before the final return.
  - Where:
    - Sync, no-payload: end of `WriteTransitionLogicSyncWithExtensions` success path, just before `return true;` and after logging `TransitionSucceeded(...)`.
    - Async/payload: end of `WriteTransitionLogicPayloadAsync`, after setting `SuccessVar = true;` and logging success, before `goto END_TRY_FIRE`.
  - Context: the already constructed transition context (`smCtx`/`__smCtx`) with final `ToState`.

- OnTransitionCompleted(ctx)
  - When: at the very end of trigger handling, after any hierarchical cascading and after success/failure handling.
  - Where:
    - Sync: at the bottom of `TryFireInternal` wrapper (just before returning the boolean), emit when a transition was attempted (for both success and failure), using the last built context or a fallback context from `OriginalStateVar`/`CurrentStateField`.
    - Async: at the end of `TryFireInternalAsync` just before `return SuccessVar;`.
  - Semantics: Always fires once per `TryFire`/`Fire` attempt (successful or not), distinct from `OnAfterTransition(ctx, success)` which remains for compatibility.

- OnUnhandledTrigger(ctx)
  - When: when no transition matches (flat) or after bubbling to root (HSM) and still no match.
  - Where: in the existing "No matching transition" branches that currently build `noTransitionCtx` and call `RunAfterTransition(..., false)`. Insert hook call immediately before `_extensionRunner.RunAfterTransition(...)`.

- OnInternalTransition(ctx)
  - When: when a transition is marked `IsInternal` and executes (no state change).
  - Where: in transition logic branches for internal transitions (flat and HSM), after the Action (and any guard) completes successfully. Insert prior to `RunAfterTransition(..., true)`.
  - Context: FromState == ToState, identifying the internal handling.

- OnBubbleToParent(ctx, handledAt)
  - When: in HSM, when an event isn’t handled in the leaf and is checked/handled in a parent state.
  - Where: inside `WriteTryFireStructureWithExtensions` HSM loop. Upon matching a parent case (we already have `var state = (TState)check;`), before executing the chosen transition, emit `OnBubbleToParent(ctx, handledAt: state)` using a context whose `FromState` is the original leaf and `ToState` is the transition’s target.

- OnInitialSubstateEntered(parentCtx, child)
  - When: after entering a composite state and resolving its initial child.
  - Where: in the HSM state-change section that detects composites:
    - The code already computes `__targetComposite`, `__resolvedIndex`, `__histMode` and logs `CompositeStateEntry(...)`.
    - Emit after `__resolvedIndex = GetCompositeEntryTarget(__targetComposite)` and before assigning `CurrentState = (__resolvedIndex)`.
    - `parentCtx` should reflect transition into the composite; `child` = `(__resolvedIndex)`.

- OnHistoryRestore(parentCtx, mode, restoredState)
  - When: when history mode is not `None` and a history entry is restored.
  - Where: in the same composite branch, directly after determining `__histMode != HistoryMode.None` and computing `__resolvedIndex`.

- OnAncestorPathChanged(ctx, exitedPath, enteredPath, lca)
  - When: for cross-branch HSM transitions that walk up to LCA and then enter down the target branch.
  - Where: in HSM sync transition code there is already logic computing `lca` and capturing the active path for logging. Reuse that segment to compute:
    - `exitedPath`: path from leaf up to but excluding `lca`.
    - `enteredPath`: path from `lca` down to the new leaf.
    - Emit before final success and after state resolution, passing spans to the hook.

Dispatching planned hooks:
- Extend `ExtensionRunner` with `RunTransitioned`, `RunTransitionCompleted`, `RunUnhandledTrigger`, `RunInternalTransition`, `RunBubbleToParent`, `RunInitialSubstateEntered`, `RunHistoryRestore`, `RunAncestorPathChanged` following the pattern of `SafeExecute` used today.
- Generated machines should call these `Run...` methods in the locations outlined above.

Compatibility guidance:
- Preserve existing `OnAfterTransition(ctx, success)` exactly where it is for backward compatibility (including the no-transition case). New hooks add finer granularity without changing current behavior.


## Implemented (New) – OnUnhandledTrigger

- Contract: `IStateMachineExtension.OnUnhandledTrigger<TContext>(TContext context)` where `TContext : IStateMachineContext`.
- Runtime: `ExtensionRunner.RunUnhandledTrigger(extensions, context)` calls the above for each extension (exception-safe, logged under `FSM_LOGGING_ENABLED`).
- Generator emission:
  - In `WriteTryFireStructureWithExtensions`:
    - When there are no transitions at all (failCtx):
      - `_extensionRunner.RunUnhandledTrigger(_extensions, failCtx);`
      - `_extensionRunner.RunAfterTransition(_extensions, failCtx, false);`
    - When no matching transition was found (noTransitionCtx):
      - `_extensionRunner.RunUnhandledTrigger(_extensions, noTransitionCtx);`
      - `_extensionRunner.RunAfterTransition(_extensions, noTransitionCtx, false);`

Note: Emission occurs in the Extensions-enabled variant; non-extensions variant continues to use only `OnAfterTransition(ctx,false)` for backward compatibility.

## Implemented (New) – OnInternalTransition

- Contract: `IStateMachineExtension.OnInternalTransition<TContext>(TContext context)` where `TContext : IStateMachineContext`.
- Runtime:
  - `ExtensionRunner.RunInternalTransition(extensions, context)` dispatches the call to all extensions.
  - `ExtensionRunner.RunAfterTransition(...)` contains a single, centralized heuristic to detect internal transitions: if `success == true` and `FromState == ToState` (via `IStateSnapshot`), it calls `OnInternalTransition(context)` exactly once before `OnAfterTransition(context, true)`.
- Generator emission:
  - To avoid double-calling, the generator no longer emits a direct call to `RunInternalTransition` for internal transitions. The single source of truth for this hook is the runtime heuristic in `RunAfterTransition`.
  - This keeps behavior consistent across sync/async and flat/HSM paths, with one well-defined emission point.
- Tests:
  - Added parity tests (Legacy/Fluent) that exercise a single internal transition and assert:
    - `TryFire` returns true,
    - state is unchanged,
    - `OnInternalTransition` fired exactly once,
    - `OnAfterTransition(..., true)` fired exactly once.
