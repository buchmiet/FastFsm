using System.Collections.Generic;

namespace Generator.FeatureDetection
{
    /// <summary>
    /// Reprezentuje zestaw funkcjonalności wykrytych w definicji maszyny stanów.
    /// Jest to "fingerprint" maszyny używany do walidacji i wyboru wariantu.
    /// </summary>
    public class FeatureSet
    {
        // Podstawowe cechy
        public bool HasOnEntryExit { get; set; }
        public bool HasPayload { get; set; }
        public bool HasMultiPayload { get; set; }
        public bool HasExtensions { get; set; }
        public bool HasLogging { get; set; }
        public bool IsAsync { get; set; }

        // Szczegółowe cechy
        public bool HasGuards { get; set; }
        public bool HasActions { get; set; }
        public bool HasInternalTransitions { get; set; }

        // Szczegóły async
        public HashSet<string> AsyncGuards { get; set; } = [];
        public HashSet<string> AsyncActions { get; set; } = [];
        public HashSet<string> AsyncOnEntry { get; set; } = [];
        public HashSet<string> AsyncOnExit { get; set; } = [];

        // Szczegóły payload
        public Dictionary<string, string> TriggerPayloadTypes { get; set; } = new();
        public string DefaultPayloadType { get; set; }

        // Statystyki (pomocne przy debugowaniu)
        public int StateCount { get; set; }
        public int TransitionCount { get; set; }
        public int GuardCount { get; set; }
        public int ActionCount { get; set; }

        /// <summary>
        /// Generuje czytelny opis wykrytych funkcjonalności
        /// </summary>
        public string GetDescription()
        {
            var features = new List<string>();

            if (IsAsync) features.Add("Async");
            if (HasPayload)
            {
                features.Add(HasMultiPayload ? "MultiPayload" : "SinglePayload");
            }
            if (HasExtensions) features.Add("Extensions");
            if (HasOnEntryExit) features.Add("OnEntry/OnExit");
            if (HasGuards) features.Add("Guards(" + GuardCount + ")");
            if (HasActions) features.Add("Actions(" + ActionCount + ")");
            if (HasLogging) features.Add("Logging");
            if (HasInternalTransitions) features.Add("InternalTransitions");

            return "FeatureSet[" + string.Join(", ", features) + "]";
        }

        /// <summary>
        /// Zwraca sugerowany wariant na podstawie wykrytych cech
        /// </summary>
        public Model.GenerationVariant GetSuggestedVariant()
        {
            // Logika identyczna jak w VariantSelector.SelectVariantBasedOnFeatures
            if (HasPayload && HasExtensions) return Model.GenerationVariant.Full;
            if (HasPayload && !HasExtensions) return Model.GenerationVariant.WithPayload;
            if (!HasPayload && HasExtensions) return Model.GenerationVariant.WithExtensions;
            if (!HasPayload && !HasExtensions && HasOnEntryExit) return Model.GenerationVariant.Basic;
            return Model.GenerationVariant.Pure;
        }
    }
}
