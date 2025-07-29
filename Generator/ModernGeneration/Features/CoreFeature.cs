using System;
using System.Linq;
using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Podstawowy moduł generujący minimalną implementację maszyny stanów (Pure/Basic variant).
    /// </summary>
    public class CoreFeature : IEmitUsings, IEmitFields, IEmitConstructor, IEmitMethods
    {
        private readonly TypeSystemHelper _typeHelper = new();
        private const string EndOfTryFireLabel = "END_TRY_FIRE";
        public void EmitUsings(GenerationContext ctx)
        {
            // Standard namespaces
            ctx.Usings.Add(NamespaceSystem);
            ctx.Usings.Add(NamespaceSystemCollectionsGeneric);
            ctx.Usings.Add(NamespaceSystemLinq);
            ctx.Usings.Add(NamespaceSystemRuntimeCompilerServices);
            ctx.Usings.Add(NamespaceStateMachineContracts);
            ctx.Usings.Add(NamespaceStateMachineRuntime);

            // Async namespaces if needed
            if (ctx.Model.GenerationConfig.IsAsync)
            {
                ctx.Usings.Add("System.Threading");
                ctx.Usings.Add("System.Threading.Tasks");
                ctx.Usings.Add("StateMachine.Exceptions");
            }

            // Type-specific namespaces
            var stateNamespaces = _typeHelper.GetRequiredNamespaces(ctx.Model.StateType);
            var triggerNamespaces = _typeHelper.GetRequiredNamespaces(ctx.Model.TriggerType);

            foreach (var ns in stateNamespaces.Union(triggerNamespaces))
            {
                ctx.Usings.Add(ns);
            }
        }

        public void EmitFields(GenerationContext ctx)
        {
            var sb = ctx.Sb;

            // Instance ID for async machines
            if (ctx.Model.GenerationConfig.IsAsync)
            {
                sb.AppendLine("private readonly string _instanceId = Guid.NewGuid().ToString();");
                sb.AppendLine();
            }
        }

        public void ContributeConstructor(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;
            var stateType = GetStateTypeForUsage(ctx);

            var baseCall = isAsync
                ? $"base(initialState, continueOnCapturedContext: {ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()})"
                : "base(initialState)";

            sb.AppendLine($"public {ctx.Model.ClassName}({stateType} initialState) : {baseCall}");
            using (sb.Block(""))  // Nie dodawaj pustej linii po tym
            {
                // Initial OnEntry dispatch for Basic variant
                if (ShouldGenerateInitialOnEntry(ctx))
                {
                    sb.AppendLine();
                    if (isAsync)
                        EmitAsyncInitialOnEntryDispatch(ctx);
                    else
                        EmitInitialOnEntryDispatch(ctx);
                }
            }
            sb.AppendLine();
        }

        public void EmitMethods(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // TryFire method
            EmitTryFireMethod(ctx);

            // CanFire method
            EmitCanFireMethod(ctx);

            // GetPermittedTriggers method
            EmitGetPermittedTriggersMethod(ctx);

            // Structural helpers if enabled
            if (ctx.Model.EmitStructuralHelpers)
            {
                EmitStructuralHelpers(ctx);
            }
        }

        private void EmitTryFireMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                // Async implementation - note: it's protected and named TryFireInternalAsync
                sb.AppendLine($"protected override async ValueTask<bool> TryFireInternalAsync({triggerType} trigger, object? payload, CancellationToken cancellationToken)");
            }
            else
            {
                // Sync implementation - public TryFire
                sb.AppendLine($"public override bool TryFire({triggerType} trigger, object? payload = null)");
            }

            using (sb.Block(""))
            {
                if (!ctx.Model.Transitions.Any())
                {
                    sb.AppendLine($"return false; {NoTransitionsComment}");
                    return;
                }

                EmitTryFireCore(ctx, stateType, triggerType);
            }
            sb.AppendLine();
        }

        private void EmitTryFireCore(GenerationContext ctx, string stateType, string triggerType)
        {
            var sb = ctx.Sb;

            sb.AppendLine($"var {OriginalStateVar} = {CurrentStateField};");
            sb.AppendLine($"bool {SuccessVar} = false;");
            sb.AppendLine();

            // Generate switch structure
            var grouped = ctx.Model.Transitions.GroupBy(t => t.FromState);

            using (sb.Block($"switch ({CurrentStateField})"))
            {
                foreach (var state in grouped)
                {
                    using (sb.Block($"case {stateType}.{_typeHelper.EscapeIdentifier(state.Key)}:"))
                    {
                        using (sb.Block("switch (trigger)"))
                        {
                            foreach (var tr in state)
                            {
                                using (sb.Block($"case {triggerType}.{_typeHelper.EscapeIdentifier(tr.Trigger)}:"))
                                {
                                    EmitTransitionLogic(ctx, tr, stateType, triggerType);
                                }
                            }
                            sb.AppendLine("default: break;");
                        }
                        sb.AppendLine("break;");
                    }
                }
                sb.AppendLine("default: break;");
            }

            sb.AppendLine();
            sb.AppendLine($"{EndOfTryFireLabel}:;");
            sb.AppendLine();

            // Log failure if not successful
            using (sb.Block($"if (!{SuccessVar})"))
            {
                // Note: We don't have logging in CoreFeature yet
            }

            sb.AppendLine($"return {SuccessVar};");
        }

        private void EmitTransitionLogic(GenerationContext ctx, TransitionModel transition, string stateType, string triggerType)
        {
            var sb = ctx.Sb;
            var hasOnEntryExit = ShouldGenerateOnEntryExit(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // Guard check - używamy GuardPolicy
            if (!string.IsNullOrEmpty(transition.GuardMethod))
            {
                // Tworzymy SliceContext dla GuardPolicy
                var sliceCtx = new SliceContext(
                    ctx,
                    PublicApiSlice.TryFire,
                    OriginalStateVar,
                    SuccessVar,
                    "null", // CoreFeature nie ma payloadu
                    GuardResultVar,
                    EndOfTryFireLabel
                );

                // Używamy GuardPolicy
                ctx.GuardPolicy.EmitGuardCheck(
                    sliceCtx,
                    transition,
                    GuardResultVar,
                    "null", // brak payloadu w Core
                    throwOnException: false // w try-catch
                );

                using (sb.Block($"if (!{GuardResultVar})"))
                {
                    sb.AppendLine($"{SuccessVar} = false;");
                    sb.AppendLine($"goto {EndOfTryFireLabel};");
                }
            }

            // OnExit
            if (!transition.IsInternal && hasOnEntryExit &&
                ctx.Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
                !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
            {
                EmitCallbackWithErrorHandling(ctx, fromStateDef.OnExitMethod, fromStateDef.OnExitIsAsync, transition);
            }

            // Action
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                EmitCallbackWithErrorHandling(ctx, transition.ActionMethod, transition.ActionIsAsync, transition);
            }

            // OnEntry
            if (!transition.IsInternal && hasOnEntryExit &&
                ctx.Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
                !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
            {
                EmitCallbackWithErrorHandling(ctx, toStateDef.OnEntryMethod, toStateDef.OnEntryIsAsync, transition);
            }

            // State change (for non-internal transitions)
            if (!transition.IsInternal)
            {
                sb.AppendLine($"{CurrentStateField} = {stateType}.{_typeHelper.EscapeIdentifier(transition.ToState)};");
            }

            sb.AppendLine($"{SuccessVar} = true;");
            sb.AppendLine($"goto {EndOfTryFireLabel};");
        }

        private void EmitCallbackWithErrorHandling(GenerationContext ctx, string methodName, bool isCallbackAsync, TransitionModel transition)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            if (isAsync)
            {
                sb.AppendLine("try");
                using (sb.Block(""))
                {
                    EmitCallbackInvocation(ctx, methodName, isCallbackAsync);
                }
                sb.AppendLine("catch (Exception)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"{SuccessVar} = false;");
                    sb.AppendLine($"goto {EndOfTryFireLabel};");
                }
            }
            else
            {
                EmitCallbackInvocation(ctx, methodName, isCallbackAsync);
            }
        }

        private void EmitCallbackInvocation(GenerationContext ctx, string methodName, bool isCallbackAsync)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            if (isAsync && isCallbackAsync)
            {
                sb.AppendLine($"await {methodName}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
            }
            else
            {
                sb.AppendLine($"{methodName}();");
            }
        }

        private void EmitCanFireMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"public override async ValueTask<bool> CanFireAsync({triggerType} trigger, CancellationToken cancellationToken = default)");
            }
            else
            {
                sb.AppendLine($"public override bool CanFire({triggerType} trigger)");
            }

            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions.GroupBy(t => t.FromState).OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        using (sb.Indent())
                        {
                            using (sb.Block("switch (trigger)"))
                            {
                                foreach (var transition in stateGroup)
                                {
                                    sb.AppendLine($"case {triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)}:");
                                    using (sb.Indent())
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            // Używamy GuardPolicy dla CanFire
                                            var sliceCtx = new SliceContext(
                                                ctx,
                                                PublicApiSlice.CanFire,
                                                "", // nie używane w CanFire
                                                "", // nie używane w CanFire
                                                "null",
                                                "guardResult",
                                                "" // nie używane w CanFire
                                            );

                                            ctx.GuardPolicy.EmitGuardCheckForCanFire(
                                                sliceCtx,
                                                transition,
                                                "guardResult",
                                                "null"
                                            );
                                        }
                                        else
                                        {
                                            sb.AppendLine("return true;");
                                        }
                                    }
                                }
                                sb.AppendLine("default: return false;");
                            }
                        }
                    }
                    sb.AppendLine("default: return false;");
                }
            }
            sb.AppendLine();
        }

        private void EmitGetPermittedTriggersMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            if (isAsync)
            {
                sb.AppendLine($"public override async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersAsync(CancellationToken cancellationToken = default)");
            }
            else
            {
                sb.AppendLine($"public override {ReadOnlyListType}<{triggerType}> GetPermittedTriggers()");
            }

            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        using (sb.Block(""))
                        {
                            // Check if any transition has a guard
                            var hasAsyncGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod) && t.GuardIsAsync);
                            var hasGuards = stateGroup.Any(t => !string.IsNullOrEmpty(t.GuardMethod));

                            if (!hasAsyncGuards && !hasGuards)
                            {
                                // No guards - return static array
                                var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                                if (triggers.Any())
                                {
                                    var triggerList = string.Join(", ", triggers.Select(t => $"{triggerType}.{_typeHelper.EscapeIdentifier(t)}"));
                                    sb.AppendLine($"return new {triggerType}[] {{ {triggerList} }};");
                                }
                                else
                                {
                                    sb.AppendLine($"return {ArrayEmptyMethod}<{triggerType}>();");
                                }
                            }
                            else
                            {
                                // Has guards - build list dynamically
                                sb.AppendLine($"var permitted = new List<{triggerType}>();");

                                foreach (var transition in stateGroup)
                                {
                                    if (!string.IsNullOrEmpty(transition.GuardMethod))
                                    {
                                        if (isAsync && transition.GuardIsAsync)
                                        {
                                            sb.AppendLine("try");
                                            using (sb.Block(""))
                                            {
                                                sb.AppendLine($"if (await {transition.GuardMethod}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()}))");
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                                }
                                            }
                                            sb.AppendLine("catch { }");
                                        }
                                        else
                                        {
                                            sb.AppendLine("bool canFire;");
                                            sb.AppendLine("try");
                                            using (sb.Block(""))
                                            {
                                                sb.AppendLine($"canFire = {transition.GuardMethod}();");
                                            }
                                            sb.AppendLine("catch");
                                            using (sb.Block(""))
                                            {
                                                sb.AppendLine("canFire = false;");
                                            }
                                            using (sb.Block("if (canFire)"))
                                            {
                                                sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                    }
                                }

                                sb.AppendLine("return permitted.Count == 0 ? ");
                                using (sb.Indent())
                                {
                                    sb.AppendLine($"{ArrayEmptyMethod}<{triggerType}>() :");
                                    sb.AppendLine("permitted.ToArray();");
                                }
                            }
                        }
                    }

                    var statesWithNoOutgoingTransitions = ctx.Model.States.Keys
                        .Except(transitionsByState.Select(g => g.Key))
                        .OrderBy(s => s);

                    foreach (var stateName in statesWithNoOutgoingTransitions)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateName)}: return {ArrayEmptyMethod}<{triggerType}>();");
                    }

                    sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerType}>();");
                }
            }
            sb.AppendLine();
        }

       

        private void EmitStructuralHelpers(GenerationContext ctx)
        {
            EmitHasTransitionMethod(ctx);
            EmitGetDefinedTriggersMethod(ctx);
        }

        private void EmitHasTransitionMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.WriteSummary("Checks if a transition is defined in the state machine structure (ignores guards)");
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
            sb.AppendLine($"public bool HasTransition({triggerType} trigger)");
            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                        if (triggers.Any())
                        {
                            sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                            using (sb.Indent())
                            {
                                using (sb.Block("switch (trigger)"))
                                {
                                    foreach (var trigger in triggers)
                                    {
                                        sb.AppendLine($"case {triggerType}.{_typeHelper.EscapeIdentifier(trigger)}: return true;");
                                    }
                                    sb.AppendLine("default: return false;");
                                }
                            }
                        }
                    }
                    sb.AppendLine("default: return false;");
                }
            }
            sb.AppendLine();
        }

        private void EmitGetDefinedTriggersMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.WriteSummary("Gets all triggers defined for the current state in the state machine structure (ignores guards)");
            sb.AppendLine($"public {ReadOnlyListType}<{triggerType}> GetDefinedTriggers()");
            using (sb.Block(""))
            {
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        var triggers = stateGroup.Select(t => t.Trigger).Distinct().ToList();
                        // Poprawione formatowanie - bez dodatkowych spacji
                        sb.Append($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}: return ");
                        if (triggers.Any())
                        {
                            var triggerList = string.Join(", ", triggers.Select(t => $"{triggerType}.{_typeHelper.EscapeIdentifier(t)}"));
                            sb.AppendLine($"new {triggerType}[] {{ {triggerList} }};");
                        }
                        else
                        {
                            sb.AppendLine($"{ArrayEmptyMethod}<{triggerType}>();");
                        }
                    }
                    sb.AppendLine($"default: return {ArrayEmptyMethod}<{triggerType}>();");
                }
            }
            sb.AppendLine();
        }

        private void EmitInitialOnEntryDispatch(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);

            sb.AppendLine(InitialOnEntryComment);
            using (sb.Block("switch (initialState)"))
            {
                foreach (var stateEntry in ctx.Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
                {
                    sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateEntry.Name)}:");
                    using (sb.Indent())
                    {
                        sb.AppendLine($"{stateEntry.OnEntryMethod}();");
                        sb.AppendLine("break;");
                    }
                }
            }
        }

        private void EmitAsyncInitialOnEntryDispatch(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);

            sb.AppendLine(InitialOnEntryComment);
            sb.AppendLine("// Note: Constructor cannot be async, so initial OnEntry is fire-and-forget");
            sb.AppendLine("_ = Task.Run(async () =>");
            using (sb.Block(""))
            {
                using (sb.Block("switch (initialState)"))
                {
                    foreach (var stateEntry in ctx.Model.States.Values.Where(s => !string.IsNullOrEmpty(s.OnEntryMethod)))
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateEntry.Name)}:");
                        using (sb.Indent())
                        {
                            EmitCallbackInvocation(ctx, stateEntry.OnEntryMethod, stateEntry.OnEntryIsAsync);
                            sb.AppendLine("break;");
                        }
                    }
                }
            }
            sb.AppendLine("});");  // Usuń dodatkowe }
        }

        // Helper methods
        private bool ShouldGenerateInitialOnEntry(GenerationContext ctx)
        {
            var config = ctx.Model.GenerationConfig;
            return config.Variant != GenerationVariant.Pure && config.HasOnEntryExit;
        }

        private bool ShouldGenerateOnEntryExit(GenerationContext ctx)
        {
            var config = ctx.Model.GenerationConfig;
            return config.Variant != GenerationVariant.Pure && config.HasOnEntryExit;
        }

        private string GetStateTypeForUsage(GenerationContext ctx)
        {
            return _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);
        }

        private string GetTriggerTypeForUsage(GenerationContext ctx)
        {
            return _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);
        }

        private string GetBaseClassName(GenerationContext ctx)
        {
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            return ctx.Model.GenerationConfig.IsAsync
                ? $"AsyncStateMachineBase<{stateType}, {triggerType}>"
                : $"StateMachineBase<{stateType}, {triggerType}>";
        }
    }
}