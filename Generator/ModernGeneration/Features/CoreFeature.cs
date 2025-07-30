using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Hooks;
using Generator.ModernGeneration.Registries;
using System;
using System.Linq;
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
            // Instance ID for async machines
            if (ctx.Model.GenerationConfig.IsAsync)
            {
                ctx.Fields.Register(new FieldSpec(
                    visibility: "private",
                    type: "string",
                    name: "_instanceId",
                    modifiers: "readonly",
                    initializer: "Guid.NewGuid().ToString()"
                ));
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

            EmitCurrentStateProperty(ctx);

            EmitFireMethod(ctx);
            // TryFire method
            EmitTryFireMethod(ctx);

            // CanFire method
            EmitCanFireMethod(ctx);

            if (ctx.Model.GenerationConfig.HasPayload)
            {
                EmitCanFireWithPayloadMethod(ctx);
            }

            // GetPermittedTriggers method
            EmitGetPermittedTriggersMethod(ctx);

            // GetPermittedTriggers z payload resolver - tylko dla wariantów z payloadem
            if (ctx.Model.GenerationConfig.HasPayload)
            {
                EmitGetPermittedTriggersWithPayloadResolver(ctx);
            }

            // Structural helpers if enabled
            if (ctx.Model.EmitStructuralHelpers)
            {
                EmitStructuralHelpers(ctx);
            }
        }

        private void EmitCanFireWithPayloadMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // Sprawdź czy to multi-payload
            var payloadFeature = ctx.Modules.OfType<IPayloadFeature>().FirstOrDefault();
            var isMultiPayload = payloadFeature?.IsMultiPayload ?? false;

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"private async ValueTask<bool> CanFireWithPayloadAsync({triggerType} trigger, object? payload, CancellationToken cancellationToken)");
            }
            else
            {
                sb.AppendLine($"private bool CanFireWithPayload({triggerType} trigger, object? payload)");
            }

            using (sb.Block(""))
            {
                // Dla multi-payload - walidacja typu
                if (isMultiPayload)
                {
                    sb.AppendLine($"if (_payloadMap.TryGetValue(trigger, out var expectedType) && payload != null && !expectedType.IsInstanceOfType(payload))");
                    using (sb.Block(""))
                    {
                        sb.AppendLine("return false;");
                    }
                }

                // Logika sprawdzania guardów z payloadem
                using (sb.Block($"switch (_currentState)"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"// DEBUG: State {stateGroup.Key} has transitions: {string.Join(", ", stateGroup.Select(t => t.Trigger))}");
                        // reszta kodu
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        using (sb.Indent())
                        {
                            using (sb.Block($"switch (trigger)"))
                            {
                                foreach (var transition in stateGroup)
                                {
                                    sb.AppendLine($"// DEBUG: Adding case for {transition.Trigger}");
                                    sb.AppendLine($"case {triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)}:");
                                    using (sb.Indent())
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            // Guard z payloadem
                                            var sctx = new SliceContext(
                                                ctx,
                                                PublicApiSlice.CanFireWithPayload,
                                                payloadVar: "payload"
                                            );

                                            ctx.GuardPolicy.EmitGuardCheckForCanFire(
                                                sctx,
                                                transition,
                                                "guardResult",
                                                "payload"
                                            );
                                        }
                                        else
                                        {
                                            // Brak guarda - zawsze dozwolone
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

        private void EmitGetPermittedTriggersWithPayloadResolver(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            sb.WriteSummary("Gets the list of triggers that can be fired in the current state with payload resolution (runtime evaluation including guards)");
            sb.WriteParam("payloadResolver", "Function to resolve payload for triggers that require it. Called only for triggers with guards expecting parameters.");

            sb.AppendLine($"public System.Collections.Generic.IReadOnlyList<{triggerType}> GetPermittedTriggers(Func<{triggerType}, object?> payloadResolver)");
            using (sb.Block(""))
            {
                sb.AppendLine("if (payloadResolver == null) throw new ArgumentNullException(nameof(payloadResolver));");
                sb.AppendLine();

                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        sb.AppendLine();
                        using (sb.Block(""))
                        {
                            sb.AppendLine($"var permitted = new List<{triggerType}>();");
                            sb.AppendLine($"// DEBUG: State {stateGroup.Key} has {stateGroup.Count()} transitions");
                            foreach (var transition in stateGroup)
                            {
                                sb.AppendLine($"// DEBUG: - {transition.Trigger} (Guard: {transition.GuardMethod ?? "none"})");
                                if (!string.IsNullOrEmpty(transition.GuardMethod))
                                {
                                    // Guard wymaga payloadu - użyj resolver
                                    if (transition.GuardExpectsPayload)
                                    {
                                        sb.AppendLine($"var payload_{transition.Trigger} = payloadResolver({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");

                                        // Generuj sprawdzenie guarda z payloadem
                                        sb.AppendLine("bool canFire;");
                                        sb.AppendLine("try");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            var payloadType = GetPayloadType(ctx, transition);
                                            sb.AppendLine($"canFire = payload_{transition.Trigger} is {payloadType} typedPayload && {transition.GuardMethod}(typedPayload);");
                                        }
                                        sb.AppendLine("catch");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            sb.AppendLine("canFire = false;");
                                        }

                                        using (sb.Block("if (canFire)"))
                                        {
                                            sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                        }
                                    }
                                    else
                                    {
                                        // Guard nie wymaga payloadu
                                        sb.AppendLine("bool canFire;");
                                        sb.AppendLine("try");
                                        sb.AppendLine();
                                        using (sb.Block(""))
                                        {
                                            sb.AppendLine($"canFire = {transition.GuardMethod}();");
                                        }
                                        sb.AppendLine("catch");
                                        sb.AppendLine();
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
                                    // Brak guarda - zawsze dozwolone
                                    sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                }
                            }

                            sb.AppendLine("return permitted.Count == 0 ? ");
                            using (sb.Indent())
                            {
                                sb.AppendLine($"System.Array.Empty<{triggerType}>() :");
                                sb.AppendLine("permitted.ToArray();");
                            }
                        }
                    }

                    // Stany bez przejść
                    var statesWithNoOutgoingTransitions = ctx.Model.States.Keys
                        .Except(transitionsByState.Select(g => g.Key))
                        .OrderBy(s => s);

                    foreach (var stateName in statesWithNoOutgoingTransitions)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateName)}: return System.Array.Empty<{triggerType}>();");
                    }

                    sb.AppendLine($"default: return System.Array.Empty<{triggerType}>();");
                }
            }
            sb.AppendLine();
        }

        // Metoda pomocnicza do określenia typu payloadu
        private string GetPayloadType(GenerationContext ctx, TransitionModel transition)
        {
            // Dla single payload
            if (!string.IsNullOrEmpty(ctx.Model.DefaultPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(ctx.Model.DefaultPayloadType, useGlobalPrefix: false);
            }

            // Dla multi-payload
            if (!string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(transition.ExpectedPayloadType, useGlobalPrefix: false);
            }

            return "object";
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
                // 0️⃣  brak przejść => szybki exit
                if (!ctx.Model.Transitions.Any())
                {
                    sb.AppendLine($"return false; {NoTransitionsComment}");
                    return;
                }

                // 1️⃣  wstrzyknięta walidacja payloadu (tylko jeśli generator obsługuje payload)
                if (hasPayload)
                {
                    ctx.Hooks.Emit(HookSlot.PayloadValidation, sb);
                    sb.AppendLine(); // przerwa dla czytelności
                }

                // 2️⃣  wspólna logika przejść (switch(state) → guardy → akcje)
                EmitTryFireCore(ctx, stateType, triggerType, payloadVar);
            }

            sb.AppendLine();
        }


        private void EmitTryFireCore(GenerationContext ctx, string stateType, string triggerType, string payloadVar)
        {
            var sb = ctx.Sb;

            // Utwórz SliceContext dla całej metody TryFire
            var sliceCtx = new SliceContext(
                ctx,
                PublicApiSlice.TryFire,
                OriginalStateVar,
                SuccessVar,
                payloadVar,      // Teraz używamy przekazanego payloadVar
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
                                    // Przekaż SliceContext do EmitTransitionLogic
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

            // Na początku metody
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

            // Action - po OnEntry
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

            // Actions zawsze mają try-catch (nawet w sync maszynach)
            // Inne callbacki mają try-catch tylko w async maszynach
            bool needsTryCatch = (callbackType == CallbackType.Action) || isAsync;

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

            // Określ czy callback oczekuje payloadu
            bool expectsPayload = callbackType switch
            {
                CallbackType.Guard => transition.GuardExpectsPayload,
                CallbackType.Action => transition.ActionExpectsPayload,
                CallbackType.OnEntry => state?.OnEntryExpectsPayload ?? false,
                CallbackType.OnExit => state?.OnExitExpectsPayload ?? false,
                _ => false
            };

            // Określ czy ma overload bezparametrowy
            bool hasParameterlessOverload = callbackType switch
            {
                CallbackType.Guard => transition.GuardHasParameterlessOverload,
                CallbackType.Action => transition.ActionHasParameterlessOverload,
                CallbackType.OnEntry => state?.OnEntryHasParameterlessOverload ?? false,
                CallbackType.OnExit => state?.OnExitHasParameterlessOverload ?? false,
                _ => false
            };

            // Jeśli nie ma payloadu w maszynie lub callback go nie oczekuje
            if (!hasPayload || !expectsPayload)
            {
                EmitSimpleCall(sb, methodName, isAsync && isCallbackAsync, ctx.Model.ContinueOnCapturedContext);
                return;
            }

            // Mamy payload i callback go oczekuje
            var payloadType = GetPayloadTypeForCallback(ctx, transition, state, callbackType);

            // Użyj różnych nazw zmiennych dla różnych callbacków aby uniknąć duplikacji
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
                // Ma overload - sprawdź typ w runtime
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
                // Tylko wersja z payloadem
                sb.AppendLine($"if ({sliceCtx.PayloadVar} is {payloadType} {varName})");
                using (sb.Block(""))
                {
                    EmitCallWithPayload(sb, methodName, varName, isAsync && isCallbackAsync, ctx.Model.ContinueOnCapturedContext);
                }
                // Jeśli payload nie pasuje, callback nie zostanie wywołany
                // To może być problemem - może powinniśmy rzucić wyjątek lub zalogować?
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
            // Dla single payload używamy DefaultPayloadType
            if (!string.IsNullOrEmpty(ctx.Model.DefaultPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(ctx.Model.DefaultPayloadType, useGlobalPrefix: false);
            }

            // Dla multi-payload używamy ExpectedPayloadType z transition
            if (!string.IsNullOrEmpty(transition.ExpectedPayloadType))
            {
                return _typeHelper.FormatTypeForUsage(transition.ExpectedPayloadType, useGlobalPrefix: false);
            }

            // Fallback
            return "object";
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
            sb.WriteSummary("Gets the list of triggers that can be fired in the current state (runtime evaluation including guards)");
            sb.WriteReturns("List of triggers that can be fired in the current state");
            sb.AppendLine(isAsync
                ? $"public override async ValueTask<{ReadOnlyListType}<{triggerType}>> GetPermittedTriggersAsync(CancellationToken cancellationToken = default)"
                : $"public override {ReadOnlyListType}<{triggerType}> GetPermittedTriggers()");

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
                                        // Sprawdź czy guard wymaga payloadu
                                        if (transition is { GuardExpectsPayload: true, GuardHasParameterlessOverload: false })
                                        {
                                            // Guard wymaga payloadu którego nie mamy
                                            sb.AppendLine("bool canFire = false; // brak payloadu → guard = false");
                                        }
                                        else
                                        {
                                            // Guard nie wymaga payloadu lub ma overload
                                            sb.AppendLine("bool canFire;");
                                            sb.AppendLine("try");
                                            sb.AppendLine();
                                            using (sb.Block(""))
                                            {
                                                if (isAsync && transition.GuardIsAsync)
                                                {
                                                    sb.AppendLine($"canFire = await {transition.GuardMethod}().ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                                                }
                                                else
                                                {
                                                    sb.AppendLine($"canFire = {transition.GuardMethod}();");
                                                }
                                            }
                                            sb.AppendLine("catch");
                                            sb.AppendLine();
                                            using (sb.Block(""))
                                            {
                                                sb.AppendLine("canFire = false;");
                                            }
                                        }
                                        using (sb.Block("if (canFire)"))
                                        {
                                            sb.AppendLine($"permitted.Add({triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)});");
                                        }
                                    }
                                    else
                                    {
                                        // Brak guarda - zawsze dozwolone
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

            // Filtruj tylko te stany, które mają OnEntry I mają wersję bezparametrową
            var statesWithParameterlessOnEntry = ctx.Model.States.Values
                .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) &&
                            (!s.OnEntryExpectsPayload || s.OnEntryHasParameterlessOverload))
                .ToList();

            if (!statesWithParameterlessOnEntry.Any())
                return; // Nic do wygenerowania

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

            // Filtruj tylko te stany, które mają OnEntry I mają wersję bezparametrową
            var statesWithParameterlessOnEntry = ctx.Model.States.Values
                .Where(s => !string.IsNullOrEmpty(s.OnEntryMethod) &&
                            (!s.OnEntryExpectsPayload || s.OnEntryHasParameterlessOverload))
                .ToList();

            if (!statesWithParameterlessOnEntry.Any())
                return; // Nic do wygenerowania

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
                            // W konstruktorze możemy wywołać tylko wersję bezparametrową
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

        private string GetStateTypeForUsage(GenerationContext ctx) => _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);

        private string GetTriggerTypeForUsage(GenerationContext ctx) => _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);

        private string GetBaseClassName(GenerationContext ctx)
        {
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);

            return ctx.Model.GenerationConfig.IsAsync
                ? $"AsyncStateMachineBase<{stateType}, {triggerType}>"
                : $"StateMachineBase<{stateType}, {triggerType}>";
        }
        private enum CallbackType
        {
            Guard,
            Action,
            OnEntry,
            OnExit
        }
        // TODO: Move to TransitionFeature
        private void EmitTransitionCore(GenerationContext ctx, SliceContext sliceCtx, TransitionModel transition)
        {
            // Wydzielona logika pojedynczego przejścia
        }

        // TODO: Move to GuardFeature  
        private void EmitGuardLogic(GenerationContext ctx, SliceContext sliceCtx, TransitionModel transition)
        {
            // Wydzielona logika guardów
        }

        // TODO: Move to OnEntryExitFeature
        private void EmitStateCallbacks(GenerationContext ctx, SliceContext sliceCtx, TransitionModel transition)
        {
            // Wydzielona logika OnEntry/OnExit
        }
    }
}