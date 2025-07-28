using System;
using Generator.ModernGeneration.Context;
using IndentedStringBuilder;

namespace Generator.ModernGeneration.Context
{
    /// <summary>
    /// Kontekst dla generowania pojedynczej "slice" (metody) maszyny stanów.
    /// Przechowuje nazwy zmiennych lokalnych, etykiety itp.
    /// </summary>
    public sealed class SliceContext
    {
        public GenerationContext Root { get; }
        public PublicApiSlice Slice { get; }

        // Standardowe nazwy zmiennych używane w metodzie
        public string OriginalStateVar { get; }
        public string SuccessVar { get; }
        public string PayloadVar { get; }
        public string GuardResultVar { get; }
        public string EndLabel { get; }

        // Skrót do StringBuilder
        public IndentedStringBuilder.IndentedStringBuilder Sb => Root.Sb;

        // Flagi pomocnicze
        public bool IsAsync => Root.Model.GenerationConfig.IsAsync;
        public bool HasPayload => Root.Model.GenerationConfig.HasPayload;

        public SliceContext(
            GenerationContext root,
            PublicApiSlice slice,
            string originalStateVar = "originalState",
            string successVar = "success",
            string payloadVar = "payload",
            string guardResultVar = "guardResult",
            string endLabel = "END_LABEL")
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Slice = slice;
            OriginalStateVar = originalStateVar;
            SuccessVar = successVar;
            PayloadVar = payloadVar;
            GuardResultVar = guardResultVar;
            EndLabel = endLabel;
        }
    }

    /// <summary>
    /// Typy metod publicznych API generowanych dla maszyny stanów
    /// </summary>
    public enum PublicApiSlice
    {
        TryFire,
        Fire,
        CanFire,
        GetPermittedTriggers,
        HasTransition,
        GetDefinedTriggers,
        TryFireInternal,        // Dla wariantów z payloadem
        CanFireWithPayload      // Dla wariantów z payloadem
    }
}