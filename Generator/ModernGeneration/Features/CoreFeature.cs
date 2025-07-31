using System;
using System.Linq;
using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Hooks;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Podstawowy moduł generujący minimalną implementację maszyny stanów.
    /// Tylko: CurrentState, Fire, TryFire, CanFire (bez payload)
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
            using (sb.Block(""))
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
            EmitCurrentStateProperty(ctx);
            EmitFireMethod(ctx);
            EmitTryFireMethod(ctx);
            EmitCanFireMethod(ctx);
        }

        private void EmitCurrentStateProperty(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);

            sb.WriteSummary("Gets the current state of the state machine");
            sb.AppendLine($"public {stateType} CurrentState => {CurrentStateField};");
            sb.AppendLine();
        }

        private void EmitFireMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.WriteSummary("Fires the specified trigger in the current state");
            sb.WriteParam("trigger", "The trigger to fire");

            if (isAsync)
            {
                sb.AppendLine($"public async Task FireAsync({triggerType} trigger, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!await TryFireAsync(trigger, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()}))"))
                    {
                        sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}'\");");
                    }
                }
            }
            else
            {
                sb.AppendLine($"public void Fire({triggerType} trigger)");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!TryFire(trigger))"))
                    {
                        sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}'\");");
                    }
                }
            }
            sb.AppendLine();
        }

        private void EmitTryFireMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;
            var hasPayload = ctx.Model.GenerationConfig.HasPayload;
            var payloadVar = hasPayload ? "payload" : "null";
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
                sb.AppendLine($"protected override async ValueTask<bool> TryFireInternalAsync({triggerType} trigger, object? payload, CancellationToken cancellationToken)");
            else
                sb.AppendLine($"public override bool TryFire({triggerType} trigger, object? payload = null)");

            using (sb.Block(""))
            {
                // 0️⃣  brak przejść => szybki exit
                if (!ctx.Model.Transitions.Any())
                {
                    sb.AppendLine($"return false; {NoTransitionsComment}");
                    return;
                }

                // 1️⃣  wstrzyknięta walidacja payloadu (tylko jeśli generator obsługuje payload)
                if (hasPayload)
                {
                    ctx.Hooks.Emit(HookSlot.PayloadValidation, sb);
                    sb.AppendLine();
                }

                // 2️⃣  wspólna logika przejść (switch(state) → guardy → akcje)
                EmitTryFireCore(ctx, stateType, triggerType, payloadVar);
            }

            sb.AppendLine();
        }

        private void EmitCanFireMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.WriteSummary("Checks if the specified trigger can be fired in the current state (runtime evaluation including guards)");
            sb.WriteParam("trigger", "The trigger to check");
            sb.WriteReturns("True if the trigger can be fired, false otherwise");
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
                                            // Guard z overloadem - wywołaj bezparametrową wersję
                                            if (transition.GuardExpectsPayload && transition.GuardHasParameterlessOverload)
                                            {
                                                sb.AppendLine("try");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    if (isAsync && transition.GuardIsAsync)
                                                    {
                                                        sb.AppendLine($"return await {transition.GuardMethod}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                                                    }
                                                    else
                                                    {
                                                        sb.AppendLine($"return {transition.GuardMethod}();");
                                                    }
                                                }
                                                sb.AppendLine("catch");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine("return false;");
                                                }
                                            }
                                            // Guard wymaga payloadu bez overloadu - nie możemy sprawdzić
                                            else if (transition.GuardExpectsPayload && !transition.GuardHasParameterlessOverload)
                                            {
                                                sb.AppendLine("try");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine("return false; // Guard expects payload but none provided");
                                                }
                                                sb.AppendLine("catch");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine("return false;");
                                                }
                                            }
                                            // Guard bez payloadu
                                            else
                                            {
                                                sb.AppendLine("try");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    if (isAsync && transition.GuardIsAsync)
                                                    {
                                                        sb.AppendLine($"return await {transition.GuardMethod}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                                                    }
                                                    else
                                                    {
                                                        sb.AppendLine($"return {transition.GuardMethod}();");
                                                    }
                                                }
                                                sb.AppendLine("catch");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine("return false;");
                                                }
                                            }
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

        // Prywatne metody pomocnicze - te same co wcześniej
        private void EmitTryFireCore(GenerationContext ctx, string stateType, string triggerType, string payloadVar)
        {
            var sb = ctx.Sb;

            var sliceCtx = new SliceContext(
                ctx,
                PublicApiSlice.TryFire,
                OriginalStateVar,
                SuccessVar,
                payloadVar,
                GuardResultVar,
                EndOfTryFireLabel
            );

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
                                    EmitTransitionLogic(ctx, sliceCtx, tr, stateType, triggerType);
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

        private void EmitTransitionLogic(
            GenerationContext ctx,
            SliceContext sliceCtx,
            TransitionModel transition,
            string stateType,
            string triggerType)
        {
            var sb = ctx.Sb;
            var hasOnEntryExit = ShouldGenerateOnEntryExit(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.AppendLine($"// DEBUG: hasOnEntryExit = {hasOnEntryExit}");
            sb.AppendLine($"// DEBUG: toState has OnEntry = {ctx.Model.States.TryGetValue(transition.ToState, out var temp) && !string.IsNullOrEmpty(temp.OnEntryMethod)}");

            // Guard check
            if (!string.IsNullOrEmpty(transition.GuardMethod))
            {
                ctx.GuardPolicy.EmitGuardCheck(
                    sliceCtx,
                    transition,
                    GuardResultVar,
                    sliceCtx.PayloadVar,
                    throwOnException: false
                );

                using (sb.Block($"if (!{GuardResultVar})"))
                {
                    sb.AppendLine($"{SuccessVar} = false;");
                    sb.AppendLine($"goto {EndOfTryFireLabel};");
                }
            }

            // OnExit - ze starego stanu
            if (!transition.IsInternal && hasOnEntryExit &&
                ctx.Model.States.TryGetValue(transition.FromState, out var fromStateDef) &&
                !string.IsNullOrEmpty(fromStateDef.OnExitMethod))
            {
                EmitCallbackWithErrorHandling(ctx, sliceCtx, fromStateDef.OnExitMethod,
                    fromStateDef.OnExitIsAsync, transition, fromStateDef, CallbackType.OnExit);
            }

            // OnEntry - do nowego stanu
            if (!transition.IsInternal && hasOnEntryExit &&
                ctx.Model.States.TryGetValue(transition.ToState, out var toStateDef) &&
                !string.IsNullOrEmpty(toStateDef.OnEntryMethod))
            {
                EmitCallbackWithErrorHandling(ctx, sliceCtx, toStateDef.OnEntryMethod,
                    toStateDef.OnEntryIsAsync, transition, toStateDef, CallbackType.OnEntry);
            }

            // Action
            if (!string.IsNullOrEmpty(transition.ActionMethod))
            {
                EmitCallbackWithErrorHandling(ctx, sliceCtx, transition.ActionMethod,
                    transition.ActionIsAsync, transition, null, CallbackType.Action);
            }

          

            // State change
            if (!transition.IsInternal)
            {
                sb.AppendLine($"{CurrentStateField} = {stateType}.{_typeHelper.EscapeIdentifier(transition.ToState)};");
            }

            sb.AppendLine($"{SuccessVar} = true;");
            sb.AppendLine($"goto {EndOfTryFireLabel};");
        }

        // Pomocnicze metody (te same co wcześniej)
        private void EmitCallbackWithErrorHandling(
            GenerationContext ctx,
            SliceContext sliceCtx,
            string methodName,
            bool isCallbackAsync,
            TransitionModel transition,
            StateModel state,
            CallbackType callbackType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            bool needsTryCatch = (callbackType == CallbackType.Action) ||
                                 (callbackType == CallbackType.OnEntry) ||
                                 (callbackType == CallbackType.OnExit) ||
                                 isAsync;

            if (needsTryCatch)
            {
                sb.AppendLine("try");
                sb.AppendLine();
                using (sb.Block(""))
                {
                    EmitCallbackInvocation(ctx, sliceCtx, methodName, isCallbackAsync, transition, state, callbackType);
                }
                sb.AppendLine("catch (Exception)");
                sb.AppendLine();
                using (sb.Block(""))
                {
                    sb.AppendLine($"{sliceCtx.SuccessVar} = false;");
                    sb.AppendLine($"goto {sliceCtx.EndLabel};");
                }
            }
            else
            {
                EmitCallbackInvocation(ctx, sliceCtx, methodName, isCallbackAsync, transition, state, callbackType);
            }
        }

        private void EmitCallbackInvocation(
            GenerationContext ctx,
            SliceContext sliceCtx,
            string methodName,
            bool isCallbackAsync,
            TransitionModel transition,
            StateModel state,
            CallbackType callbackType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;
            var hasPayload = ctx.Model.GenerationConfig.HasPayload;

            bool expectsPayload = callbackType switch
            {
                CallbackType.Guard => transition.GuardExpectsPayload,
                CallbackType.Action => transition.ActionExpectsPayload,
                CallbackType.OnEntry => state?.OnEntryExpectsPayload ?? false,
                CallbackType.OnExit => state?.OnExitExpectsPayload ?? false,
                _ => false
            };

            bool hasParameterlessOverload = callbackType switch
            {
                CallbackType.Guard => transition.GuardHasParameterlessOverload,
                CallbackType.Action => transition.ActionHasParameterlessOverload,
                CallbackType.OnEntry => state?.OnEntryHasParameterlessOverload ?? false,
                CallbackType.OnExit => state?.OnExitHasParameterlessOverload ?? false,
                _ => false
            };

            if (!hasPayload || !expectsPayload)
            {
                EmitSimpleCall(sb, methodName, isAsync && isCallbackAsync, ctx.Model.ContinueOnCapturedContext);
                return;
            }

            var payloadType = GetPayloadTypeForCallback(ctx, transition, state, callbackType);

            var varName = callbackType switch
            {
                CallbackType.Guard => "typedGuardPayload",
                CallbackType.Action => "typedActionPayload",
                CallbackType.OnEntry => "typedEntryPayload",
                CallbackType.OnExit => "typedExitPayload",
                _ => "typedPayload"
            };

            if (hasParameterlessOverload)
            {
                sb.AppendLine($"if ({sliceCtx.PayloadVar} is {payloadType} {varName})");
                using (sb.Block(""))
                {
                    EmitCallWithPayload(sb, methodName, varName, isAsync && isCallbackAsync, ctx.Model.ContinueOnCapturedContext);
                }
                sb.AppendLine("else");
                using (sb.Indent())
                {
                    EmitSimpleCall(sb, methodName, isAsync && isCallbackAsync, ctx.Model.ContinueOnCapturedContext);
                }
            }
            else
            {
                sb.AppendLine($"if ({sliceCtx.PayloadVar} is {payloadType} {varName})");
                using (sb.Block(""))
                {
                    EmitCallWithPayload(sb, methodName, varName, isAsync && isCallbackAsync, ctx.Model.ContinueOnCapturedContext);
                }
            }
        }

        // Metody pomocnicze
        private void EmitSimpleCall(IndentedStringBuilder.IndentedStringBuilder sb, string methodName, bool isAsync, bool continueOnCapturedContext)
        {
            if (isAsync)
            {
                sb.AppendLine($"await {methodName}().ConfigureAwait({continueOnCapturedContext.ToString().ToLowerInvariant()});");
            }
            else
            {
                sb.AppendLine($"{methodName}();");
            }
        }

        private void EmitCallWithPayload(IndentedStringBuilder.IndentedStringBuilder sb, string methodName, string payloadVar, bool isAsync, bool continueOnCapturedContext)
        {
            if (isAsync)
            {
                sb.AppendLine($"await {methodName}({payloadVar}).ConfigureAwait({continueOnCapturedContext.ToString().ToLowerInvariant()});");
            }
            else
            {
                sb.AppendLine($"{methodName}({payloadVar});");
            }
        }

        private string GetPayloadTypeForCallback(GenerationContext ctx, TransitionModel transition, StateModel state, CallbackType callbackType)
        {
            if (!string.IsNullOrEmpty(ctx.Model.DefaultPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(ctx.Model.DefaultPayloadType, useGlobalPrefix: false);
            }

            if (!string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(transition.ExpectedPayloadType, useGlobalPrefix: false);
            }

            return "object";
        }

        private void EmitInitialOnEntryDispatch(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);

            sb.AppendLine(InitialOnEntryComment);

            var statesWithParameterlessOnEntry = ctx.Model.States.Values
                .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) &&
                            (!s.OnEntryExpectsPayload || s.OnEntryHasParameterlessOverload))
                .ToList();

            if (!statesWithParameterlessOnEntry.Any())
                return;

            using (sb.Block("switch (initialState)"))
            {
                foreach (var stateEntry in statesWithParameterlessOnEntry)
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

            var statesWithParameterlessOnEntry = ctx.Model.States.Values
                .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) &&
                            (!s.OnEntryExpectsPayload || s.OnEntryHasParameterlessOverload))
                .ToList();

            if (!statesWithParameterlessOnEntry.Any())
                return;

            sb.AppendLine("_ = Task.Run(async () =>");
            using (sb.Block(""))
            {
                using (sb.Block("switch (initialState)"))
                {
                    foreach (var stateEntry in statesWithParameterlessOnEntry)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateEntry.Name)}:");
                        using (sb.Indent())
                        {
                            EmitSimpleCall(sb, stateEntry.OnEntryMethod, stateEntry.OnEntryIsAsync, ctx.Model.ContinueOnCapturedContext);
                            sb.AppendLine("break;");
                        }
                    }
                }
            }
            sb.AppendLine("});");
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

        private string GetStateTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);

        private string GetTriggerTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);

        private enum CallbackType
        {
            Guard,
            Action,
            OnEntry,
            OnExit
        }
    }
}