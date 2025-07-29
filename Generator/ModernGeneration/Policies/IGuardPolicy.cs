// File: ModernGeneration/Policies/IGuardPolicy.cs
using Generator.Model;
using Generator.ModernGeneration.Context;

namespace Generator.ModernGeneration.Policies
{
    /// <summary>
    /// Polityka generowania wywołań guardów - centralizuje całą logikę guardów.
    /// </summary>
    public interface IGuardPolicy
    {
        /// <summary>
        /// Generuje kod sprawdzający guard dla TryFire/Fire.
        /// </summary>
        /// <param name="sctx">Kontekst slice'a</param>
        /// <param name="transition">Model przejścia z informacjami o guardzie</param>
        /// <param name="resultVar">Nazwa zmiennej dla wyniku (np. "guardResult")</param>
        /// <param name="payloadExpr">Wyrażenie payloadu (np. "payload" lub "null")</param>
        /// <param name="throwOnException">Czy propagować wyjątki (true dla TryFire, false dla CanFire)</param>
        void EmitGuardCheck(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr,
            bool throwOnException);

        /// <summary>
        /// Generuje kod sprawdzający guard dla GetPermittedTriggers.
        /// </summary>
        void EmitGuardCheckForPermitted(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr);

        /// <summary>
        /// Generuje kod sprawdzający guard dla CanFire (uproszczona wersja).
        /// </summary>
        void EmitGuardCheckForCanFire(
            SliceContext sctx,
            TransitionModel transition,
            string resultVar,
            string payloadExpr);
    }
}