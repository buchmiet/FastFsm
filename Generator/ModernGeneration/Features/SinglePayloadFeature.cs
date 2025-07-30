// -----------------------------------------------------------------------------
//  Features/SinglePayloadFeature.cs
// -----------------------------------------------------------------------------
using System;
using Generator.ModernGeneration.Context;
using Generator.ModernGeneration.Hooks;
using Generator.ModernGeneration.Registries;
using Generator.Infrastructure;             // ← TypeSystemHelper
using System.Linq;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Moduł obsługujący jeden, wspólny typ payloadu.
    /// </summary>
    public sealed class SinglePayloadFeature :
        IPayloadFeature,
        IFeatureModule,         // ← żeby Director.InitModule(this, ctx) zadziałał
        IEmitUsings
    {
        private readonly string _payloadType;
        private readonly TypeSystemHelper _typeHelper = new();

        #region flagi IPayloadFeature
        public bool IsSinglePayload => true;
        public bool IsMultiPayload => false;
        #endregion

        // ───────────────────────── constructor ─────────────────────────
        public SinglePayloadFeature(string payloadType)
        {
            if (string.IsNullOrWhiteSpace(payloadType))
                throw new ArgumentNullException(nameof(payloadType));

            _payloadType = payloadType;
        }

        // ───────────────────────── IFeatureModule ──────────────────────
        /// <summary>Wywoływane przez Director zaraz po utworzeniu modułu.</summary>
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
                var call = $"{methodName}(payload as {_payloadType})";
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

        // ───────────────────────── helpers ─────────────────────────────
        private void RegisterUsings(GenerationContext ctx)
        {
            foreach (var ns in _typeHelper.GetRequiredNamespaces(_payloadType))
                ctx.Usings.Add(ns);
        }

        private void RegisterHooks(GenerationContext ctx)
        {
            ctx.Hooks.Register(HookSlot.PayloadValidation, sb =>
            {
                sb.AppendLine($"if (payload is not {_payloadType})");
                sb.AppendLine("    return false;");
            });
        }
    }
}
