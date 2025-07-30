using Generator.ModernGeneration.Context;

namespace Generator.ModernGeneration.Features
{
    /// <summary>
    /// Wspólny interfejs dla modułów obsługujących payload.
    /// </summary>
    public interface IPayloadFeature : IFeatureModule
    {
        /// <summary>
        /// Czy moduł obsługuje pojedynczy typ payloadu dla wszystkich triggerów.
        /// </summary>
        bool IsSinglePayload { get; }

        /// <summary>
        /// Czy moduł obsługuje różne typy payloadu per trigger.
        /// </summary>
        bool IsMultiPayload { get; }

        /// <summary>
        /// Emituje walidację payloadu (głównie dla Multi).
        /// </summary>
        void EmitPayloadValidation(SliceContext sctx);

        /// <summary>
        /// Emituje wywołanie metody z obsługą payloadu (guard/action/onEntry/onExit).
        /// </summary>
        void EmitPayloadAwareCall(
            SliceContext sctx,
            string methodName,
            bool isMethodAsync,
            bool expectsPayload,
            bool hasParameterlessOverload,
            string? expectedPayloadType);
    }
}