using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Infrastructure;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Hooks;
using Generator.ModernGeneration.Registries;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł obsługujący mapę Trigger → typ payloadu dla multi-payload.
    /// </summary>
    public sealed class MultiPayloadFeature :
        IPayloadFeature,
        IFeatureModule,
        IEmitUsings,
        IEmitFields,
        IEmitMethods
    {
        private const string MapFieldName = PayloadMapField;
        private readonly TypeSystemHelper _typeHelper = new();

        #region IPayloadFeature
        public bool IsSinglePayload => false;
        public bool IsMultiPayload => true;
        #endregion

        public MultiPayloadFeature() { }

        // ───────────────────────── IFeatureModule ──────────────────────
        public void Initialize(GenerationContext ctx)
        {
            RegisterUsings(ctx);
            RegisterHooks(ctx);
        }

        // ───────────────────────── IPayloadFeature ─────────────────────
        public void EmitPayloadValidation(SliceContext ctx)
        {
            // Hook załatwia walidację w TryFire
        }

        public void EmitPayloadAwareCall(
            SliceContext sctx,
            string methodName,
            bool expectsPayload,
            bool hasParameterlessOverload,
            bool isMethodAsync,
            string? expectedPayloadType)
        {
            var sb = sctx.Sb;

            if (!expectsPayload || expectedPayloadType == null)
            {
                // Metoda nie oczekuje payloadu
                var call = $"{methodName}()";
                sb.AppendLine(isMethodAsync
                    ? $"await {call}.ConfigureAwait({sctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});"
                    : $"{call};");
                return;
            }

            // Metoda oczekuje payloadu
            var payloadType = _typeHelper.FormatTypeForUsage(expectedPayloadType, useGlobalPrefix: false);
            var varName = GetTypedPayloadVarName(methodName);

            if (hasParameterlessOverload)
            {
                sb.AppendLine($"if ({sctx.PayloadVar} is {payloadType} {varName})");
                using (sb.Block(""))
                {
                    var call = $"{methodName}({varName})";
                    sb.AppendLine(isMethodAsync
                        ? $"await {call}.ConfigureAwait({sctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});"
                        : $"{call};");
                }
                sb.AppendLine("else");
                using (sb.Indent())
                {
                    var call = $"{methodName}()";
                    sb.AppendLine(isMethodAsync
                        ? $"await {call}.ConfigureAwait({sctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});"
                        : $"{call};");
                }
            }
            else
            {
                sb.AppendLine($"if ({sctx.PayloadVar} is {payloadType} {varName})");
                using (sb.Block(""))
                {
                    var call = $"{methodName}({varName})";
                    sb.AppendLine(isMethodAsync
                        ? $"await {call}.ConfigureAwait({sctx.Root.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});"
                        : $"{call};");
                }
            }
        }

        // ───────────────────────── IEmitUsings ─────────────────────────
        public void EmitUsings(GenerationContext _) { /* już dodane w Initialize */ }

        // ───────────────────────── IEmitFields ─────────────────────────
        public void EmitFields(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);

            // Generate payload map field - używamy new() jak w legacy
            sb.AppendLine($"private static readonly Dictionary<{triggerType}, Type> {MapFieldName} = new()");
            using (sb.Block(""))
            {
                foreach (var kvp in ctx.Model.TriggerPayloadTypes)
                {
                    var typeForTypeof = _typeHelper.FormatForTypeof(kvp.Value);
                    sb.AppendLine($"{{ {triggerType}.{_typeHelper.EscapeIdentifier(kvp.Key)}, typeof({typeForTypeof}) }},");
                }
            }
            sb.AppendLine(";");
            sb.AppendLine();
        }

        // ───────────────────────── IEmitMethods ─────────────────────────
        public void EmitMethods(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // TryFireInternal z walidacją typu
            EmitTryFireInternal(sb, triggerType, isAsync);

            // TryFire<TPayload>
            EmitGenericTryFire(sb, triggerType, isAsync);

            // Fire<TPayload>
            EmitGenericFire(sb, triggerType);

            // CanFire<TPayload>
            EmitGenericCanFire(sb, triggerType);

            // CanFire(trigger, object?)
            EmitCanFireWithObject(sb, triggerType);

            // CanFireWithPayload (private helper)
            EmitCanFireWithPayloadMethod(ctx);

            // Async sync throw methods
            if (isAsync)
            {
                EmitSyncThrowMethods(sb, triggerType);
            }
        }

        // ───────────────────────── private methods ─────────────────────
        private void EmitTryFireInternal(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType, bool isAsync)
        {
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
                sb.AppendLine($"private async ValueTask<bool> TryFireInternal({triggerType} trigger, object? payload)");
            else
                sb.AppendLine($"private bool TryFireInternal({triggerType} trigger, object? payload)");

            using (sb.Block(""))
            {
                // Multi-payload: walidacja typu PRZED sprawdzeniem guarda
                // Legacy używa && (payload == null || !expectedType.IsInstanceOfType(payload))
                // Modern używa is not null
                sb.AppendLine($"if ({MapFieldName}.TryGetValue(trigger, out var expectedType) && (payload == null || !expectedType.IsInstanceOfType(payload)))");
                using (sb.Block(""))
                {
                    sb.AppendLine("return false; // wrong payload type");
                }

                // Sprawdź guard
                var canFireCall = isAsync
                    ? "await CanFireWithPayloadAsync(trigger, payload)"
                    : "CanFireWithPayload(trigger, payload)";

                sb.AppendLine($"if (!{canFireCall})");
                sb.AppendLine("    return false;");

                // Deleguj do base class
                sb.AppendLine(isAsync
                    ? "return await base.TryFireInternalAsync(trigger, payload);"
                    : "return base.TryFireInternal(trigger, payload);");
            }
            sb.AppendLine();
        }

        private void EmitGenericTryFire(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType, bool isAsync)
        {
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"public ValueTask<bool> TryFire<TPayload>({triggerType} trigger, TPayload payload)");
            }
            else
            {
                sb.AppendLine($"public bool TryFire<TPayload>({triggerType} trigger, TPayload payload)");
            }

            using (sb.Block(""))
            {
                sb.AppendLine("return TryFireInternal(trigger, payload);");
            }
            sb.AppendLine();
        }

        private void EmitGenericFire(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType)
        {
            sb.AppendLine($"public void Fire<TPayload>({triggerType} trigger, TPayload payload)");
            using (sb.Block(""))
            {
                using (sb.Block($"if (!TryFire(trigger, payload))"))
                {
                    sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                }
            }
            sb.AppendLine();
        }

        private void EmitGenericCanFire(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType)
        {
            sb.WriteSummary("Checks if the specified trigger can be fired with the given payload (runtime evaluation incl. guards)");
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
            sb.AppendLine($"public bool CanFire<TPayload>({triggerType} trigger, TPayload payload)");
            using (sb.Block(""))
            {
                using (sb.Block($"if ({MapFieldName}.TryGetValue(trigger, out var expectedType) && !expectedType.IsInstanceOfType(payload))"))
                {
                    sb.AppendLine("return false;");
                }
                sb.AppendLine("return CanFireWithPayload(trigger, payload);");
            }
            sb.AppendLine();
        }

        private void EmitCanFireWithObject(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType)
        {
            sb.WriteSummary("Checks if the specified trigger can be fired with an optional payload (runtime evaluation incl. guards)");
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
            sb.AppendLine($"public bool CanFire({triggerType} trigger, object? payload = null)");
            using (sb.Block(""))
            {
                sb.AppendLine("return CanFireWithPayload(trigger, payload);");
            }
            sb.AppendLine();
        }

        private void EmitCanFireWithPayloadMethod(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var stateType = GetStateTypeForUsage(ctx);
            var triggerType = GetTriggerTypeForUsage(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"private async ValueTask<bool> CanFireWithPayloadAsync({triggerType} trigger, object? payload)");
            }
            else
            {
                sb.AppendLine($"private bool CanFireWithPayload({triggerType} trigger, object? payload)");
            }

            using (sb.Block(""))
            {
                // Walidacja typu dla multi-payload
                sb.AppendLine($"if ({MapFieldName}.TryGetValue(trigger, out var expectedType) && payload != null && !expectedType.IsInstanceOfType(payload))");
                using (sb.Block(""))
                {
                    sb.AppendLine("return false;");
                }

                // Logika sprawdzania guardów z payloadem
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
                                            if (transition.GuardExpectsPayload)
                                            {
                                                // Guard z payloadem - używamy ExpectedPayloadType
                                                var payloadType = !string.IsNullOrEmpty(transition.ExpectedPayloadType)
                                                    ? _typeHelper.FormatTypeForUsage(transition.ExpectedPayloadType, useGlobalPrefix: false)
                                                    : "object";

                                                // Bezpośrednie sprawdzenie jak w legacy
                                                sb.AppendLine($"try");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine($"return payload is {payloadType} typedGuardPayload && {transition.GuardMethod}(typedGuardPayload);");
                                                }
                                                sb.AppendLine("catch");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine("return false;");
                                                }
                                            }
                                            else
                                            {
                                                // Guard bez payloadu
                                                sb.AppendLine($"try");
                                                sb.AppendLine();
                                                using (sb.Block(""))
                                                {
                                                    sb.AppendLine($"return {transition.GuardMethod}();");
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

        private void EmitSyncThrowMethods(IndentedStringBuilder.IndentedStringBuilder sb, string triggerType)
        {
            // TryFire<TPayload> - sync throw
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");
            sb.AppendLine($"public bool TryFire<TPayload>({triggerType} trigger, TPayload payload)");
            using (sb.Block(""))
            {
                sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
            }
            sb.AppendLine();

            // Fire<TPayload> - sync throw
            sb.AppendLine($"public void Fire<TPayload>({triggerType} trigger, TPayload payload)");
            using (sb.Block(""))
            {
                sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
            }
            sb.AppendLine();

            // CanFire<TPayload> - sync throw
            sb.AppendLine($"public bool CanFire<TPayload>({triggerType} trigger, TPayload payload)");
            using (sb.Block(""))
            {
                sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
            }
            sb.AppendLine();

            // CanFire z object - sync throw
            sb.AppendLine($"public bool CanFire({triggerType} trigger, object? payload = null)");
            using (sb.Block(""))
            {
                sb.AppendLine("throw new SyncCallOnAsyncMachineException();");
            }
            sb.AppendLine();
        }

        // ───────────────────────── helpers ─────────────────────────────
        private void RegisterUsings(GenerationContext ctx)
        {
            var payloadNamespaces = ctx.Model.TriggerPayloadTypes
                .SelectMany(kvp => _typeHelper.GetRequiredNamespaces(kvp.Value))
                .Distinct();

            foreach (var ns in payloadNamespaces)
                ctx.Usings.Add(ns);

            ctx.Usings.Add("System");
            ctx.Usings.Add("System.Collections.Generic");
        }

        private void RegisterHooks(GenerationContext ctx)
        {
            ctx.Hooks.Register(HookSlot.PayloadValidation, sb =>
            {
                // Walidacja w TryFire
                sb.AppendLine($"if ({MapFieldName}.TryGetValue(trigger, out var expected) &&");
                sb.AppendLine("    payload is not null && !expected.IsInstanceOfType(payload))");
                sb.AppendLine("    return false;");
            });
        }

        private string GetTypedPayloadVarName(string methodName)
        {
            if (methodName.Contains("Guard") || methodName.Contains("CanSend")) return "typedGuardPayload";
            if (methodName.Contains("Action") || methodName.Contains("Process") || methodName.Contains("Start")) return "typedActionPayload";
            if (methodName.Contains("Entry")) return "typedEntryPayload";
            if (methodName.Contains("Exit")) return "typedExitPayload";
            return "typedPayload";
        }

        private string GetStateTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.StateType, useGlobalPrefix: false);

        private string GetTriggerTypeForUsage(GenerationContext ctx) =>
            _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);
    }
}