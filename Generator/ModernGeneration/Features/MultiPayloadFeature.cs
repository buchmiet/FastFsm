// -----------------------------------------------------------------------------
//  Features/MultiPayloadFeature.cs
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Hooks;
using Generator.ModernGeneration.Registries;
using Generator.Infrastructure;            // ← TypeSystemHelper
using Generator.Model;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł obsługujący mapę Trigger → typ payloadu.
    /// </summary>
    public sealed class MultiPayloadFeature :
        IPayloadFeature,
        IFeatureModule,
        IEmitUsings,
        IEmitFields
    {
        private const string MapFieldName =Strings.PayloadMapField;

        private readonly TypeSystemHelper _typeHelper = new();

        #region flagi IPayloadFeature
        public bool IsSinglePayload => false;
        public bool IsMultiPayload => true;
        #endregion

        // ───────────────────────── constructor ─────────────────────────
        // (bez parametrów – tak wywołuje go Twój ModernGenerator)
        public MultiPayloadFeature() { }

        // ───────────────────────── IFeatureModule ──────────────────────
        public void Initialize(GenerationContext ctx)
        {
            RegisterUsings(ctx);
            RegisterHooks(ctx);
        }

        // ───────────────────────── IPayloadFeature ─────────────────────
        public void EmitPayloadValidation(SliceContext _) { /* hook załatwia wszystko */ }

        public void EmitPayloadAwareCall(
            SliceContext sctx,
            string methodName,
            bool isMethodAsync,
            bool expectsPayload,
            bool hasParameterlessOverload,
            string? expectedPayloadType)
        {
            var sb = sctx.Root.Sb;

            if (expectsPayload)
            {
                var call = $"{methodName}(payload!)";
                sb.AppendLine(isMethodAsync ? $"await {call}.ConfigureAwait(false);" : $"{call};");
            }
            else if (hasParameterlessOverload)
            {
                var call = $"{methodName}()";
                sb.AppendLine(isMethodAsync ? $"await {call}.ConfigureAwait(false);" : $"{call};");
            }
        }

        // ───────────────────────── IEmitUsings ─────────────────────────
        public void EmitUsings(GenerationContext _) { /* już dodane w Initialize */ }

        // ───────────────────────── IEmitFields ─────────────────────────
        public void EmitFields(GenerationContext ctx)
        {
            var triggerType = _typeHelper.FormatTypeForUsage(
                ctx.Model.TriggerType,
                useGlobalPrefix: false);

            var mapEntries = string.Join(",\n",
                ctx.Model.TriggerPayloadTypes.Select(kvp =>
                    $"    {{ {triggerType}.{kvp.Key}, typeof({_typeHelper.FormatTypeForUsage(kvp.Value, false)}) }}"));

            var initializer =
                $"new Dictionary<{triggerType}, Type>\n{{\n{mapEntries}\n}}";

            ctx.Fields.Register(new FieldSpec(
                visibility: "private",
                type: $"Dictionary<{triggerType}, Type>",
                name: MapFieldName,
                modifiers: "static readonly",
                initializer: initializer));
        }

        // ───────────────────────── helpers ─────────────────────────────
        private void RegisterUsings(GenerationContext ctx)
        {
            var payloadNamespaces = ctx.Model.TriggerPayloadTypes
                                         .SelectMany(kvp => _typeHelper.GetRequiredNamespaces(kvp.Value))
                                         .Distinct();

            foreach (var ns in payloadNamespaces)
                ctx.Usings.Add(ns);

            // również potrzebujemy przestrzeni nazw dla Dictionary i Type
            ctx.Usings.Add("System");
            ctx.Usings.Add("System.Collections.Generic");
        }

        private void RegisterHooks(GenerationContext ctx)
        {
            var triggerType = _typeHelper.FormatTypeForUsage(
                ctx.Model.TriggerType,
                useGlobalPrefix: false);

            ctx.Hooks.Register(HookSlot.PayloadValidation, sb =>
            {
                sb.AppendLine($"if ({MapFieldName}.TryGetValue(trigger, out var expected) &&");
                sb.AppendLine("    payload is not null && !expected.IsInstanceOfType(payload))");
                sb.AppendLine("    return false;");
            });
        }
    }
}
