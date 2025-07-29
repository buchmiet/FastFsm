using System;
using System.Linq;
using Generator.Infrastructure;
using Generator.Model;
using Generator.ModernGeneration.Context;
using static Generator.Strings;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł obsługujący maszyny z pojedynczym typem payloadu dla wszystkich triggerów.
    /// </summary>
    public class SinglePayloadFeature : IPayloadFeature, IEmitUsings, IEmitFields, IEmitMethods
    {
        private readonly TypeSystemHelper _typeHelper = new();
        private readonly string _payloadType;

        public SinglePayloadFeature(string payloadType)
        {
            _payloadType = payloadType ?? throw new ArgumentNullException(nameof(payloadType));
        }

        public bool IsSinglePayload => true;
        public bool IsMultiPayload => false;

        public void EmitUsings(GenerationContext ctx)
        {
            // Dodaj namespace dla typu payloadu
            var payloadNamespaces = _typeHelper.GetRequiredNamespaces(_payloadType);
            foreach (var ns in payloadNamespaces)
            {
                ctx.Usings.Add(ns);
            }
        }

        public void EmitFields(GenerationContext ctx)
        {
            // Single payload nie potrzebuje dodatkowych pól
            // Typ payloadu jest znany w czasie kompilacji
        }

        public void EmitMethods(GenerationContext ctx)
        {
            var sb = ctx.Sb;
            var triggerType = GetTriggerType(ctx);
            var payloadTypeFormatted = GetFormattedPayloadType();
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            // Skip if payload type is 'object' to avoid duplicates
            if (payloadTypeFormatted == "object")
                return;

            // TryFire overload
            EmitTryFireOverload(ctx, triggerType, payloadTypeFormatted);

            // Fire overload
            EmitFireOverload(ctx, triggerType, payloadTypeFormatted);

            // CanFire overload
            EmitCanFireOverload(ctx, triggerType, payloadTypeFormatted);
        }

        private void EmitTryFireOverload(GenerationContext ctx, string triggerType, string payloadType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"public async ValueTask<bool> TryFireAsync({triggerType} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return await TryFireInternalAsync(trigger, {PayloadVar}, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                }
            }
            else
            {
                sb.AppendLine($"public bool TryFire({triggerType} trigger, {payloadType} {PayloadVar})");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return TryFireInternal(trigger, {PayloadVar});");
                }
            }
            sb.AppendLine();
        }

        private void EmitFireOverload(GenerationContext ctx, string triggerType, string payloadType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            if (isAsync)
            {
                sb.AppendLine($"public async Task FireAsync({triggerType} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!await TryFireAsync(trigger, {PayloadVar}, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()}))"))
                    {
                        sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
            }
            else
            {
                sb.AppendLine($"public void Fire({triggerType} trigger, {payloadType} {PayloadVar})");
                using (sb.Block(""))
                {
                    using (sb.Block($"if (!TryFire(trigger, {PayloadVar}))"))
                    {
                        sb.AppendLine($"throw new InvalidOperationException($\"No valid transition from state '{{CurrentState}}' on trigger '{{trigger}}' with payload of type '{payloadType}'\");");
                    }
                }
            }
            sb.AppendLine();
        }

        private void EmitCanFireOverload(GenerationContext ctx, string triggerType, string payloadType)
        {
            var sb = ctx.Sb;
            var isAsync = ctx.Model.GenerationConfig.IsAsync;

            sb.WriteSummary($"Checks if the specified trigger can be fired with the given {payloadType} payload");
            sb.AppendLine($"[{MethodImplAttribute}({AggressiveInliningAttribute})]");

            if (isAsync)
            {
                sb.AppendLine($"public async ValueTask<bool> CanFireAsync({triggerType} trigger, {payloadType} {PayloadVar}, CancellationToken cancellationToken = default)");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return await CanFireWithPayloadAsync(trigger, {PayloadVar}, cancellationToken).ConfigureAwait({ctx.Model.ContinueOnCapturedContext.ToString().ToLowerInvariant()});");
                }
            }
            else
            {
                sb.AppendLine($"public bool CanFire({triggerType} trigger, {payloadType} {PayloadVar})");
                using (sb.Block(""))
                {
                    sb.AppendLine($"return CanFireWithPayload(trigger, {PayloadVar});");
                }
            }
            sb.AppendLine();
        }

        public void EmitPayloadValidation(SliceContext sctx)
        {
            // Single payload nie wymaga walidacji typu w runtime
            // Typ jest sprawdzany w czasie kompilacji
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
            var payloadType = GetFormattedPayloadType();

            if (!expectsPayload)
            {
                // Metoda nie oczekuje payloadu - wywołaj bez parametrów
                EmitSimpleCall(sctx, methodName, isMethodAsync);
                return;
            }

            // Metoda oczekuje payloadu
            if (hasParameterlessOverload)
            {
                // Ma overload bezparametrowy - użyj if/else
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
                // Nie ma overloadu - sprawdź typ i wywołaj
                using (sb.Block($"if ({sctx.PayloadVar} is {payloadType} typedPayload)"))
                {
                    EmitCallWithPayload(sctx, methodName, isMethodAsync, "typedPayload");
                }
                // Jeśli payload nie pasuje - nic nie rób (lub możesz rzucić wyjątek)
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

        private string GetFormattedPayloadType()
        {
            return _typeHelper.FormatTypeForUsage(_payloadType, useGlobalPrefix: false);
        }
    }
}