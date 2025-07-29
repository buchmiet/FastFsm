using System;
using System.Linq;
using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Registries;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł obsługujący maszyny z różnymi typami payloadu per trigger.
    /// </summary>
    public class MultiPayloadFeature : IPayloadFeature, IEmitUsings, IEmitFields, IEmitMethods
    {
        private readonly TypeSystemHelper _typeHelper = new();

        public bool IsSinglePayload => false;
        public bool IsMultiPayload => true;

        public void EmitUsings(GenerationContext ctx)
        {
            // Dodaj namespace'y dla wszystkich typów payloadu
            foreach (var payloadType in ctx.Model.TriggerPayloadTypes.Values.Distinct())
            {
                var namespaces = _typeHelper.GetRequiredNamespaces(payloadType);
                foreach (var ns in namespaces)
                {
                    ctx.Usings.Add(ns);
                }
            }
        }

        public void EmitFields(GenerationContext ctx)
        {
            // Rejestruj pole mapy payload
            var triggerType = GetTriggerType(ctx);

            ctx.Fields.Register(new FieldSpec(
                visibility: "private static readonly",
                type: $"Dictionary<{triggerType}, Type>",
                name: PayloadMapField,
                initializer: "new()"
            ));

            // Generuj inicjalizację mapy
            EmitPayloadMapInitialization(ctx);
        }

        private void EmitPayloadMapInitialization(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = GetTriggerType(ctx);

            sb.AppendLine();
            sb.AppendLine("// Static constructor to initialize payload map");
            sb.AppendLine($"static {ctx.Model.ClassName}()");
            using (sb.Block(""))
            {
                foreach (var kvp in ctx.Model.TriggerPayloadTypes)
                {
                    var trigger = kvp.Key;
                    var payloadType = kvp.Value;
                    var typeForTypeof = _typeHelper.FormatForTypeof(payloadType);

                    sb.AppendLine($"{PayloadMapField}.Add({triggerType}.{_typeHelper.EscapeIdentifier(trigger)}, typeof({typeForTypeof}));");
                }
            }
            sb.AppendLine();
        }

        public void EmitMethods(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = GetTriggerType(ctx);
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // Generic TryFire<TPayload> overload
            EmitGenericTryFireOverload(ctx, triggerType);

            // Generic Fire<TPayload> overload
            EmitGenericFireOverload(ctx, triggerType);

            // Generic CanFire<TPayload> overload
            EmitGenericCanFireOverload(ctx, triggerType);
        }

        private void EmitGenericTryFireOverload(GenerationContext ctx, string triggerType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"public async ValueTask<bool> TryFireAsync<TPayload>({triggerType} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return await TryFireInternalAsync(trigger, {PayloadVar}, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                }
            }
            else
            {
                sb.AppendLine($"public bool TryFire<TPayload>({triggerType} trigger, TPayload {PayloadVar})");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
                }
            }
            sb.AppendLine();
        }

        private void EmitGenericFireOverload(GenerationContext ctx, string triggerType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            if (isAsync)
            {
                sb.AppendLine($"public async Task FireAsync<TPayload>({triggerType} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!await TryFireAsync(trigger, {PayloadVar}, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()}))"))
                    {
                        sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
            }
            else
            {
                sb.AppendLine($"public void Fire<TPayload>({triggerType} trigger, TPayload {PayloadVar})");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                    {
                        sb.AppendLine("throw new InvalidOperationException($\"No valid transition from state '{CurrentState}' on trigger '{trigger}' with payload of type '{typeof(TPayload).Name}'\");");
                    }
                }
            }
            sb.AppendLine();
        }

        private void EmitGenericCanFireOverload(GenerationContext ctx, string triggerType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.WriteSummary("Checks if the specified trigger can be fired with the given payload");
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"public async ValueTask<bool> CanFireAsync<TPayload>({triggerType} trigger, TPayload {PayloadVar}, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    // Walidacja typu
                    using (sb.Block($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && !expectedType.IsInstanceOfType({PayloadVar}))"))
                    {
                        sb.AppendLine("return false;");
                    }
                    sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                }
            }
            else
            {
                sb.AppendLine($"public bool CanFire<TPayload>({triggerType} trigger, TPayload {PayloadVar})");
                using (sb.Block(""))
                {
                    // Walidacja typu
                    using (sb.Block($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && !expectedType.IsInstanceOfType({PayloadVar}))"))
                    {
                        sb.AppendLine("return false;");
                    }
                    sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
                }
            }
            sb.AppendLine();
        }

        public void EmitPayloadValidation(SliceContext sctx)
        {
            var sb = sctx.Sb;

            // Walidacja typu payloadu dla multi-payload
            sb.AppendLine("// Payload-type validation for multi-payload variant");
            using (sb.Block($"if ({PayloadMapField}.TryGetValue(trigger, out var expectedType) && " +
                           $"({sctx.PayloadVar} == null || !expectedType.IsInstanceOfType({sctx.PayloadVar})))"))
            {
                // TODO: Dodać logging gdy będzie LoggingFeature
                sb.AppendLine($"return false; // wrong payload type");
            }
            sb.AppendLine();
        }

        public void EmitPayloadAwareCall(
            SliceContext sctx,
            string methodName,
            bool isMethodAsync,
            bool expectsPayload,
            bool hasParameterlessOverload,
            string? expectedPayloadType)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;

            if (!expectsPayload)
            {
                // Metoda nie oczekuje payloadu
                EmitSimpleCall(sctx, methodName, isMethodAsync);
                return;
            }

            // Multi-payload - musimy znać konkretny typ
            if (string.IsNullOrEmpty(expectedPayloadType))
            {
                // Nie znamy typu - użyj overloadu bezparametrowego jeśli istnieje
                if (hasParameterlessOverload)
                {
                    EmitSimpleCall(sctx, methodName, isMethodAsync);
                }
                // W przeciwnym razie - skip (nie możemy wywołać)
                return;
            }

            var payloadType = _typeHelper.FormatTypeForUsage(expectedPayloadType, useGlobalPrefix: false);

            if (hasParameterlessOverload)
            {
                // Ma overload - użyj if/else
                sb.AppendLine($"if ({sctx.PayloadVar} is {payloadType} typedPayload)");
                using (sb.Block(""))
                {
                    EmitCallWithPayload(sctx, methodName, isMethodAsync, "typedPayload");
                }
                sb.AppendLine("else");
                using (sb.Indent())
                {
                    EmitSimpleCall(sctx, methodName, isMethodAsync);
                }
            }
            else
            {
                // Nie ma overloadu - sprawdź typ
                using (sb.Block($"if ({sctx.PayloadVar} is {payloadType} typedPayload)"))
                {
                    EmitCallWithPayload(sctx, methodName, isMethodAsync, "typedPayload");
                }
            }
        }

        private void EmitSimpleCall(SliceContext sctx, string methodName, bool isMethodAsync)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;

            if (sctx.IsAsync && isMethodAsync)
            {
                sb.AppendLine($"{asyncPolicy.AwaitKeyword(true)}{methodName}(){asyncPolicy.ConfigureAwait()};");
            }
            else
            {
                sb.AppendLine($"{methodName}();");
            }
        }

        private void EmitCallWithPayload(SliceContext sctx, string methodName, bool isMethodAsync, string payloadVar)
        {
            var sb = sctx.Sb;
            var asyncPolicy = sctx.Root.AsyncPolicy;

            if (sctx.IsAsync && isMethodAsync)
            {
                sb.AppendLine($"{asyncPolicy.AwaitKeyword(true)}{methodName}({payloadVar}){asyncPolicy.ConfigureAwait()};");
            }
            else
            {
                sb.AppendLine($"{methodName}({payloadVar});");
            }
        }

        private string GetTriggerType(GenerationContext ctx)
        {
            return _typeHelper.FormatTypeForUsage(ctx.Model.TriggerType, useGlobalPrefix: false);
        }
    }
}