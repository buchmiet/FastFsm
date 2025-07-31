using Generator.Infrastructure;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Hooks;
using System;
using System.Linq;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł dla maszyn z pojedynczym typem payloadu
    /// </summary>
    public class SinglePayloadFeature : IEmitMethods, IPayloadFeature
    {
        private readonly TypeSystemHelper _typeHelper = new();
        private readonly string _payloadType;

        public bool IsSinglePayload => true;
        public bool IsMultiPayload => false;

        public SinglePayloadFeature(string payloadType)
        {
            _payloadType = payloadType ?? throw new ArgumentNullException(nameof(payloadType));
        }

        public void EmitMethods(GenerationContext ctx)
        {
            EmitCanFireWithPayloadMethod(ctx);
            EmitPayloadInternalMethods(ctx);
        }

        public void EmitPayloadValidation(SliceContext ctx)
        {
            // Single payload nie potrzebuje walidacji typu w runtime
        }

        public void EmitPayloadAwareCall(
            SliceContext ctx,
            string methodName,
            bool expectsPayload,
            bool hasParameterlessOverload,
            bool isAsync,
            string? payloadType)
        {
            var sb = ctx.Sb;

            if (!expectsPayload)
            {
                // Metoda nie oczekuje payloadu
                if (isAsync)
                {
                    sb.AppendLine($"await {methodName}().ConfigureAwait({ctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                }
                else
                {
                    sb.AppendLine($"{methodName}();");
                }
                return;
            }

            // Metoda oczekuje payloadu
            var typedPayloadVar = "typedPayload";
            var finalPayloadType = _typeHelper.FormatTypeForUsage(_payloadType, useGlobalPrefix: false);

            if (hasParameterlessOverload)
            {
                // Ma overload bezparametrowy
                sb.AppendLine($"if ({ctx.PayloadVar} is {finalPayloadType} {typedPayloadVar})");
                using (sb.Block(""))
                {
                    if (isAsync)
                    {
                        sb.AppendLine($"await {methodName}({typedPayloadVar}).ConfigureAwait({ctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                    }
                    else
                    {
                        sb.AppendLine($"{methodName}({typedPayloadVar});");
                    }
                }
                sb.AppendLine("else");
                using (sb.Indent())
                {
                    if (isAsync)
                    {
                        sb.AppendLine($"await {methodName}().ConfigureAwait({ctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                    }
                    else
                    {
                        sb.AppendLine($"{methodName}();");
                    }
                }
            }
            else
            {
                // Tylko wersja z payloadem
                sb.AppendLine($"if ({ctx.PayloadVar} is {finalPayloadType} {typedPayloadVar})");
                using (sb.Block(""))
                {
                    if (isAsync)
                    {
                        sb.AppendLine($"await {methodName}({typedPayloadVar}).ConfigureAwait({ctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                    }
                    else
                    {
                        sb.AppendLine($"{methodName}({typedPayloadVar});");
                    }
                }
            }
        }

        private void EmitCanFireWithPayloadMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;
            var payloadType = _typeHelper.FormatTypeForUsage(_payloadType, useGlobalPrefix: false);

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
                using (sb.Block($"switch ({CurrentStateField})"))
                {
                    var transitionsByState = ctx.Model.Transitions
                        .GroupBy(t => t.FromState)
                        .OrderBy(g => g.Key);

                    foreach (var stateGroup in transitionsByState)
                    {
                        sb.AppendLine($"case {stateType}.{_typeHelper.EscapeIdentifier(stateGroup.Key)}:");
                        using (sb.Indent())
                        {
                            using (sb.Block($"switch (trigger)"))
                            {
                                foreach (var transition in stateGroup)
                                {
                                    sb.AppendLine($"case {triggerType}.{_typeHelper.EscapeIdentifier(transition.Trigger)}:");
                                    using (sb.Indent())
                                    {
                                        if (!string.IsNullOrEmpty(transition.GuardMethod))
                                        {
                                            // Legacy style - zawsze deklarujemy guardResult
                                            sb.AppendLine("bool guardResult;");
                                            sb.AppendLine("try");
                                            sb.AppendLine();
                                            using (sb.Block(""))
                                            {
                                                if (transition.GuardExpectsPayload && transition.GuardHasParameterlessOverload)
                                                {
                                                    // Guard z overloadem - if/else
                                                    sb.AppendLine($"if (payload is {payloadType} typedGuardPayload)");
                                                    sb.AppendLine();
                                                    using (sb.Block(""))
                                                    {
                                                        if (isAsync && transition.GuardIsAsync)
                                                        {
                                                            sb.AppendLine($"guardResult = await {transition.GuardMethod}(typedGuardPayload).ConfigureAwait(_continueOnCapturedContext);");
                                                        }
                                                        else
                                                        {
                                                            sb.AppendLine($"guardResult = {transition.GuardMethod}(typedGuardPayload);");
                                                        }
                                                    }
                                                    sb.AppendLine("else");
                                                    using (sb.Indent())
                                                    {
                                                        if (isAsync && transition.GuardIsAsync)
                                                        {
                                                            sb.AppendLine($"guardResult = await {transition.GuardMethod}().ConfigureAwait(_continueOnCapturedContext);");
                                                        }
                                                        else
                                                        {
                                                            sb.AppendLine($"guardResult = {transition.GuardMethod}();");
                                                        }
                                                    }
                                                }
                                                else if (transition.GuardExpectsPayload)
                                                {
                                                    // Guard oczekuje payloadu - sprawdzamy typ
                                                    sb.AppendLine($"guardResult = payload is {payloadType} typedGuardPayload && {transition.GuardMethod}(typedGuardPayload);");
                                                }
                                                else
                                                {
                                                    // Guard nie oczekuje payloadu
                                                    if (isAsync && transition.GuardIsAsync)
                                                    {
                                                        sb.AppendLine($"guardResult = await {transition.GuardMethod}().ConfigureAwait(_continueOnCapturedContext);");
                                                    }
                                                    else
                                                    {
                                                        sb.AppendLine($"guardResult = {transition.GuardMethod}();");
                                                    }
                                                }
                                            }
                                            sb.AppendLine("catch");
                                            sb.AppendLine();
                                            using (sb.Block(""))
                                            {
                                                sb.AppendLine("guardResult = false;");
                                            }
                                            sb.AppendLine("return guardResult;");
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

        private void EmitPayloadInternalMethods(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // TryFireInternal
            EmitTryFireInternal(sb, triggerType, isAsync);

            // Single payload specific methods
            EmitSinglePayloadMethods(sb, triggerType, ctx);

            // Async sync throw methods
            if (isAsync)
            {
                EmitSyncThrowMethods(sb, triggerType);
            }
        }

        private void EmitTryFireInternal(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType, bool isAsync)
        {
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                // Dla async - generuj tylko wrapper do CanFireWithPayloadAsync
                sb.AppendLine($"private async ValueTask<bool> TryFireInternal({triggerType} trigger, object? payload, CancellationToken cancellationToken)");
                using (sb.Block(""))
                {
                    // Sprawdź guard
                    sb.AppendLine($"if (!await CanFireWithPayloadAsync(trigger, payload, cancellationToken))");
                    sb.AppendLine("    return false;");

                    // Deleguj do base class - tutaj używamy TryFireInternalAsync!
                    sb.AppendLine("return await base.TryFireInternalAsync(trigger, payload, cancellationToken);");
                }
            }
            else
            {
                sb.AppendLine($"private bool TryFireInternal({triggerType} trigger, object? payload)");
                using (sb.Block(""))
                {
                    // Sprawdź guard
                    sb.AppendLine($"if (!CanFireWithPayload(trigger, payload))");
                    sb.AppendLine("    return false;");

                    // Deleguj do base class
                    sb.AppendLine("return base.TryFireInternal(trigger, payload);");
                }
            }
            sb.AppendLine();
        }

        private void EmitSinglePayloadMethods(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType, GenerationContext ctx)
        {
            var payloadType = _typeHelper.FormatTypeForUsage(_payloadType, useGlobalPrefix: false);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // Skip jeśli payload type to 'object' - unikamy duplikatów
            if (payloadType == "object") return;

            if (isAsync)
            {
                // Dla async - generuj async wersje
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public async ValueTask<bool> TryFireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return await TryFireInternalAsync(trigger, payload, cancellationToken).ConfigureAwait(_continueOnCapturedContext);");
                }
                sb.AppendLine();

                // Fire async
                sb.AppendLine($"public async Task FireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"if (!await TryFireAsync(trigger, payload, cancellationToken).ConfigureAwait(_continueOnCapturedContext))");
                    using (sb.Block(""))
                    {
                        sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
                sb.AppendLine();

                // CanFire async z konkretnym typem
                sb.WriteSummary("Asynchronously checks if the specified trigger can be fired with the given payload (runtime evaluation incl. guards)");
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public async ValueTask<bool> CanFireAsync({triggerType} trigger, {payloadType} payload, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, payload, cancellationToken).ConfigureAwait(_continueOnCapturedContext);");
                }
                sb.AppendLine();

                // CanFire async z object?
                sb.WriteSummary("Asynchronously checks if the specified trigger can be fired with an optional payload (runtime evaluation incl. guards)");
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public async ValueTask<bool> CanFireAsync({triggerType} trigger, object? payload = null, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, payload, cancellationToken).ConfigureAwait(_continueOnCapturedContext);");
                }
                sb.AppendLine();
            }
            else
            {
                // Sync wersje - tak jak masz obecnie
                // TryFire z konkretnym typem
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public bool TryFire({triggerType} trigger, {payloadType} payload)");
                using (sb.Block(""))
                {
                    sb.AppendLine("return TryFireInternal(trigger, payload);");
                }
                sb.AppendLine();

                // Fire z konkretnym typem
                sb.AppendLine($"public void Fire({triggerType} trigger, {payloadType} payload)");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!TryFire(trigger, payload))"))
                    {
                        sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
                sb.AppendLine();

                // CanFire z konkretnym typem
                sb.WriteSummary("Checks if the specified trigger can be fired with the given payload (runtime evaluation incl. guards)");
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public bool CanFire({triggerType} trigger, {payloadType} payload)");
                using (sb.Block(""))
                {
                    sb.AppendLine("return CanFireWithPayload(trigger, payload);");
                }
                sb.AppendLine();

                // CanFire z object?
                sb.WriteSummary("Checks if the specified trigger can be fired with an optional payload (runtime evaluation incl. guards)");
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public bool CanFire({triggerType} trigger, object? payload = null)");
                using (sb.Block(""))
                {
                    sb.AppendLine("return CanFireWithPayload(trigger, payload);");
                }
                sb.AppendLine();
            }
        }

        private void EmitSyncThrowMethods(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType)
        {
            var payloadType = _typeHelper.FormatTypeForUsage(_payloadType, useGlobalPrefix: false);

            // Skip if payload type is 'object' to avoid duplicates
            if (payloadType != "object")
            {
                sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
                sb.AppendLine($"public bool TryFire({triggerType} trigger, {payloadType} payload)");
                using (sb.Block(""))
                {
                    sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                sb.AppendLine();

                sb.AppendLine($"public void Fire({triggerType} trigger, {payloadType} payload)");
                using (sb.Block(""))
                {
                    sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                sb.AppendLine();

                sb.AppendLine($"public bool CanFire({triggerType} trigger, {payloadType} payload)");
                using (sb.Block(""))
                {
                    sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
                }
                sb.AppendLine();
            }
        }

        private string GetStateTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);

        private string GetTriggerTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);
    }
}