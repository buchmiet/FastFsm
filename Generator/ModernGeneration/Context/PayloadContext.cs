using System;
using Generator.Model;

namespace Generator.ModernGeneration.Context
{
    /// <summary>
    /// Rozszerzenie SliceContext o informacje specyficzne dla payload.
    /// </summary>
    public class PayloadContext
    {
        /// <summary>
        /// Czy maszyna obsługuje payload.
        /// </summary>
        public bool HasPayload { get; }

        /// <summary>
        /// Czy to wariant z pojedynczym typem payloadu.
        /// </summary>
        public bool IsSinglePayload { get; }

        /// <summary>
        /// Czy to wariant z wieloma typami payloadu.
        /// </summary>
        public bool IsMultiPayload { get; }

        /// <summary>
        /// Typ payloadu dla single-payload variant.
        /// </summary>
        public string? SinglePayloadType { get; }

        /// <summary>
        /// Aktualnie przetwarzana tranzycja (dla określenia typu payloadu w multi).
        /// </summary>
        public TransitionModel? CurrentTransition { get; set; }

        public PayloadContext(StateMachineModel model)
        {
            var config = model.GenerationConfig;
            HasPayload = config.HasPayload;

            if (HasPayload)
            {
                IsMultiPayload = model.TriggerPayloadTypes.Count > 0;
                IsSinglePayload = !IsMultiPayload;

                if (IsSinglePayload)
                {
                    SinglePayloadType = model.DefaultPayloadType;
                }
            }
        }

        /// <summary>
        /// Pobiera typ payloadu dla aktualnej tranzycji.
        /// </summary>
        public string? GetPayloadTypeForCurrentTransition()
        {
            if (!HasPayload)
                return null;

            if (IsSinglePayload)
                return SinglePayloadType;

            // Multi-payload - użyj typu z tranzycji
            return CurrentTransition?.ExpectedPayloadType;
        }
    }

    /// <summary>
    /// Rozszerzenia dla SliceContext.
    /// </summary>
    public static class SliceContextExtensions
    {
        private const string PayloadContextKey = "PayloadContext";

        /// <summary>
        /// Pobiera lub tworzy PayloadContext dla danego SliceContext.
        /// </summary>
        public static PayloadContext GetPayloadContext(this SliceContext sctx)
        {
            // W prawdziwej implementacji użylibyśmy słownika w GenerationContext
            // Na potrzeby przykładu tworzymy za każdym razem
            return new PayloadContext(sctx.Root.Model);
        }

        /// <summary>
        /// Sprawdza czy slice obsługuje payload.
        /// </summary>
        public static bool IsPayloadSlice(this SliceContext sctx)
        {
            return sctx.Slice switch
            {
                PublicApiSlice.TryFireInternal => true,
                PublicApiSlice.CanFireWithPayload => true,
                _ => false
            };
        }
    }
}